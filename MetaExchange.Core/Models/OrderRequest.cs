namespace MetaExchange.Core.Models;

public class OrderRequest
{
    public OrderType Type { get; set; }
    public decimal Amount { get; set; }
}

public enum OrderType
{
    Buy,
    Sell
}