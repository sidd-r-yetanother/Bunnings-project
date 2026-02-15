using Newtonsoft.Json;

namespace Bunnings.Domain.Models;

public class OrderLine
{
    [JsonProperty("id")]
    public string ProductId { get; set; } = string.Empty;

    [JsonProperty("quantity")]
    public int Quantity { get; set; }
}