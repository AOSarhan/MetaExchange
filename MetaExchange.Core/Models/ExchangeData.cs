namespace MetaExchange.Core.Models;

public class ExchangeData
{
    public string Id { get; init; } = string.Empty;
    public required AvailableFunds AvailableFunds { get; init; }
    public required OrderBook OrderBook { get; init; }
}