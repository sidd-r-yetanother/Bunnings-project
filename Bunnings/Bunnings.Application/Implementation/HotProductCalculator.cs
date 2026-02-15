using Bunnings.Application.Interface;
using Bunnings.Domain.Models;
using Bunnings.Domain.Results;

namespace Bunnings.Application.Implementation;

/// <summary>
/// Calculates the "Sizzling Hot Product" results from orders and products JSON streams.
/// Produces:
/// - A daily top product
/// - A top product across the last 3 days
/// </summary>
public class HotProductCalculator : IHotProductCalculator
{
    // Per assumptions
    private static readonly DateOnly Today = new(2021, 7, 21);

    private readonly IJsonFileProcessor _jsonFileProcessor;
    private readonly IOrderProcessor _orderProcessor;

    public HotProductCalculator(IJsonFileProcessor jsonFileProcessor, IOrderProcessor orderProcessor)
    {
        _jsonFileProcessor = jsonFileProcessor;
        _orderProcessor = orderProcessor;
    }

    public async Task<HotProductsResult> Calculate(Stream ordersFile, Stream productsFile)
    {
        var orders = await _jsonFileProcessor.DeserializeListAsync<Order>(ordersFile);
        var products = await _jsonFileProcessor.DeserializeListAsync<Product>(productsFile);
       
        var nameByProductId = products.ToDictionary(p => p.Id, p => p.Name);

        var trackByDate = _orderProcessor.ProcessOrders(orders);

        return new HotProductsResult(
            GetDailyTopProduct(trackByDate, nameByProductId),
            GetLast3DaysTopProduct(trackByDate, nameByProductId)
        );
    }
    
    private static List<DailyTopProduct> GetDailyTopProduct(
        Dictionary<DateOnly, Dictionary<string, int>> trackByDate,
        Dictionary<string,string> nameByProductId)
    {
        var result = new List<DailyTopProduct>();
        foreach (var (date, dayCounts) in trackByDate.OrderBy(kvp => kvp.Key))
        {
            result.Add(new DailyTopProduct(
                date, 
                PickTopProduct(dayCounts, nameByProductId))
            );
        }
        return result;
    }
    
    private static PeriodTopProduct GetLast3DaysTopProduct(
        Dictionary<DateOnly, Dictionary<string, int>> trackByDate,
        Dictionary<string,string> nameByProductId)
    {
        var periodFrom = Today.AddDays(-2);
        var periodTotals = new Dictionary<string, int>();

        for (var d = periodFrom; d <= Today; d = d.AddDays(1))
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

        return new PeriodTopProduct(periodFrom, Today, PickTopProduct(periodTotals, nameByProductId));
    }
    
    // Picks the top product ID using:
    // 1) highest count
    // 2) tie-break: alphabetical by product name
    private static string PickTopProduct(
        Dictionary<string, int> countsByProductId,
        Dictionary<string, string> nameByProductId)
    {
        var top = countsByProductId
            .Select(kvp =>
            {
                var name = nameByProductId.GetValueOrDefault(kvp.Key, "");
                return new { Count = kvp.Value, Name = name };
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .First();

        return top.Name;
    }
}
