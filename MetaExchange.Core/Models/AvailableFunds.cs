using System.Text.Json.Serialization;

namespace MetaExchange.Core.Models;

public class AvailableFunds
{
    [JsonPropertyName("Crypto")]
    public decimal BtcBalance { get; init; }

    [JsonPropertyName("Euro")]
    public decimal EurBalance { get; init; }
}