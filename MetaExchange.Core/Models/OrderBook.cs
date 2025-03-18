namespace MetaExchange.Core.Models;

public class OrderBook
{
    public required List<OrderWrapper> Bids { get; init; }
    public required List<OrderWrapper> Asks { get; init; }
}

public class OrderWrapper
{
    public required Order Order { get; init; }
}