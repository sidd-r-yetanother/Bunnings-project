using System.Text.Json.Serialization;

namespace Bunnings.Domain.Models;

public class Order
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("entries")]
    public List<OrderLine> Entries { get; set; } = [];

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; }
}