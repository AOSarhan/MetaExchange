using MetaExchange.Core;
using MetaExchange.Core.Models;

namespace MetaExchange.Console;

public class App(IMetaExchange metaExchange, IExchangeLoader exchangeLoader)
{
    public async Task RunAsync(string[] args)
    {
        var request = new OrderRequest { Type = OrderType.Buy, Amount = 10m };
        try
        {
            var exchanges = exchangeLoader.LoadExchanges();
            var executionOrders = metaExchange.ProcessOrder(exchanges, request);

            System.Console.WriteLine("Execution Plan:");
            foreach (var executionOrder in executionOrders)
                System.Console.WriteLine(
                    $"Exchange: {executionOrder.Exchange}, Amount: {executionOrder.Amount:F8} BTC, Price: {executionOrder.Price} EUR");

            // Calculate total cost or proceeds
            var totalEur = executionOrders.Sum(order => order.Amount * order.Price);
            if (request.Type == OrderType.Buy)
            {
                System.Console.WriteLine($"\nTotal Cost: {totalEur:F2} EUR to buy {request.Amount:F8} BTC");
                var executedBtc = executionOrders.Sum(order => order.Amount);
                if (executedBtc < request.Amount)
                    System.Console.WriteLine(
                        $"Note: Only {executedBtc:F8} BTC bought due to insufficient liquidity or funds.");
            }
            else
            {
                System.Console.WriteLine($"\nTotal Proceeds: {totalEur:F2} EUR from selling {request.Amount:F8} BTC");
                var executedBtc = executionOrders.Sum(order => order.Amount);
                if (executedBtc < request.Amount)
                    System.Console.WriteLine(
                        $"Note: Only {executedBtc:F8} BTC sold due to insufficient liquidity or funds.");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
        }

        await Task.CompletedTask;
    }
}