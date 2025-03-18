namespace MetaExchange.Core.Models;

public class ExecutionOrder
{
    public string Exchange { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal Price { get; init; }
}