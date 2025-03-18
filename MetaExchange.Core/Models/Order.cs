using System.Text.Json.Serialization;

namespace MetaExchange.Core.Models;

public class Order
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderType Type { get; init; }
    public decimal Amount { get; init; }
    public decimal Price { get; init; }
}