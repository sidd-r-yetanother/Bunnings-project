using Bunnings.Application.Interface;
using Bunnings.Domain.Models;
using Bunnings.Domain.Results;

namespace Bunnings.Application.Implementation;

public class HotProductCalculator : IHotProductCalculator
{
    // Per assumptions
    private static readonly DateOnly Today = new(2021, 7, 21);
    private const string DateFormat = "dd/MM/yyyy";

    private readonly IJsonFileProcessor _jsonFileProcessor;

    public HotProductCalculator(IJsonFileProcessor jsonFileProcessor)
    {
        _jsonFileProcessor = jsonFileProcessor;
    }

    public async Task<HotProductsResult> Calculate(Stream ordersFile, Stream productsFile)
    {
        var orders = await _jsonFileProcessor.DeserializeListAsync<Order>(ordersFile);
        var products = await _jsonFileProcessor.DeserializeListAsync<Product>(productsFile);
       
        var nameByProductId = products.ToDictionary(p => p.Id, p => p.Name);

        // Used to find the original "completed" order when we see a cancellation
        var orderById = orders
            .Where(x => x.Status == OrderStatus.Completed)
            .GroupBy(x => x.OrderId)
            .ToDictionary(g => g.Key, g => g.First());

        // Business rule: only count once per (day, customer, product) even across multiple orders.
        var purchased = new HashSet<(DateOnly date, string customerId, string productId)>();

        // Day -> (ProductId -> salesCount)
        var trackByDate = new Dictionary<DateOnly, Dictionary<string, int>>();

        foreach (var order in orders)
        {
            var date = ParseDate(order.Date);

            if (order.Status == OrderStatus.Completed)
            {
                foreach (var productId in order.Entries.Select(x => x.ProductId).Distinct())
                {
                    var key = (date, order.CustomerId, productId);

                    // Multiple orders of same product by same customer on same day count once.
                    if (!purchased.Add(key))
                    {
                        continue;
                    }

                    var dayCounts = GetOrCreateDayCounts(trackByDate,date);
                    dayCounts[productId] = dayCounts.GetValueOrDefault(productId, 0) + 1;
                }
            }
            else if (order.Status == OrderStatus.Canceled)
            {
                // Cancellation record has no entries
                if (!orderById.TryGetValue(order.OrderId, out var originalOrder))
                {
                    continue;
                }

                var originalDate = ParseDate(originalOrder.Date);

                foreach (var productId in originalOrder.Entries.Select(x => x.ProductId).Distinct())
                {
                    var key = (originalDate, originalOrder.CustomerId, productId);

                    // If we have not yet counted it, we skip this
                    if (!purchased.Contains(key))
                    {
                        continue;
                    }
                    
                    var dayCounts = GetOrCreateDayCounts(trackByDate,originalDate);
                    dayCounts[productId] = dayCounts.GetValueOrDefault(productId, 0) - 1;
                    
                    purchased.Remove(key);
                }
            }
        }

        // Build daily top list (sorted by date)
        var dailyTop = new List<DailyTopProduct>();

        foreach (var (date, dayCounts) in trackByDate.OrderBy(kvp => kvp.Key))
        {
            dailyTop.Add(new DailyTopProduct(
                date, 
                nameByProductId.GetValueOrDefault(PickTopProductId(dayCounts, nameByProductId), "Unknown"))
            );
        }

        // Last 3 days. Top product based on aggregated totals
        var periodFrom = Today.AddDays(-2);
        var periodTo = Today;

        var periodTotals = new Dictionary<string, int>();

        for (var d = periodFrom; d <= periodTo; d = d.AddDays(1))
        {
            if (!trackByDate.TryGetValue(d, out var dayCounts))
            {
                continue;
            }

            foreach (var kv in dayCounts)
            {
                periodTotals[kv.Key] = periodTotals.GetValueOrDefault(kv.Key, 0) + kv.Value;
            }
        }

        var periodTopProductId = PickTopProductId(periodTotals, nameByProductId);

        var periodTopName = nameByProductId.GetValueOrDefault(periodTopProductId, "Unknown");

        return new HotProductsResult(
            dailyTop,
            new PeriodTopProduct(periodFrom, periodTo, periodTopName)
        );
    }

    private static DateOnly ParseDate(string dateString) => DateOnly.ParseExact(dateString, DateFormat);

    private static Dictionary<string, int> GetOrCreateDayCounts(
        Dictionary<DateOnly, Dictionary<string, int>> trackByDate,
        DateOnly date)
    {
        if (!trackByDate.TryGetValue(date, out var dayCounts))
        {
            trackByDate[date] = dayCounts = new Dictionary<string, int>();
        }

        return dayCounts;
    }

    // Picks the top product ID using:
    // 1) highest count
    // 2) tie-break: alphabetical by product name
    private static string PickTopProductId(
        Dictionary<string, int> countsByProductId,
        Dictionary<string, string> nameByProductId)
    {
        return countsByProductId
            .OrderByDescending(x => x.Value)
            .ThenBy(x => nameByProductId.GetValueOrDefault(x.Key, ""))
            .Select(x => x.Key)
            .First();
    }
}
