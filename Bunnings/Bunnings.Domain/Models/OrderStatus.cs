using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace Bunnings.Domain.Models;

[JsonConverter(typeof(StringEnumConverter))]
public enum OrderStatus
{
    [EnumMember(Value = "cancelled")]
    Canceled = 0,

    [EnumMember(Value = "completed")]
    Completed = 1
}