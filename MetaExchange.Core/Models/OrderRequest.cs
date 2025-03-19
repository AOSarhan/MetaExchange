namespace MetaExchange.Core.Models;

public class OrderRequest
{
    public OrderType Type { get; init; }
    public decimal Amount { get; init; }
}

public enum OrderType
{
    Buy,
    Sell
}