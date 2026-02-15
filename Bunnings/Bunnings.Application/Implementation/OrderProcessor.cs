using Bunnings.Application.Interface;
using Bunnings.Domain.Models;

namespace Bunnings.Application.Implementation;

public class OrderProcessor : IOrderProcessor
{
    private const string DateFormat = "dd/MM/yyyy";
    
    /// <summary>
    /// Aggregates order data into daily sales counts per product.
    /// </summary>
    public Dictionary<DateOnly, Dictionary<string, int>> ProcessOrders(List<Order> orders)
    {
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

                    if (!purchased.Remove(key))
                    {
                        continue;
                    }
                    
                    var dayCounts = GetOrCreateDayCounts(trackByDate,originalDate);
                    dayCounts[productId] = dayCounts.GetValueOrDefault(productId, 0) - 1;
                }
            }
        }
        return trackByDate;
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
}