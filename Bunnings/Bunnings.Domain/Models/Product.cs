using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Bunnings.Domain.Models;

public class Product
{
    public Product(string id, string name)
    {
        Id = id;
        Name = name;
    }
    
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
}