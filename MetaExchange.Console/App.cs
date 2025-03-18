using MetaExchange.Core;
using MetaExchange.Core.Models;

namespace MetaExchange.Console;

public class App(IMetaExchange metaExchange, IExchangeLoader exchangeLoader)
{
    public async Task RunAsync(string[] args)
    {
        var request = new OrderRequest { Type = OrderType.Buy, Amount = 9m };
        try
        {
            var exchanges = exchangeLoader.LoadExchanges();
            var orders = metaExchange.ProcessOrderLargeSet(exchanges, request);
            System.Console.WriteLine("Execution Plan:");
            foreach (var order in orders)
            {
                System.Console.WriteLine(
                    $"Exchange: {order.Exchange}, Amount: {order.Amount:F8} BTC, Price: {order.Price} EUR");
            }

            System.Console.WriteLine($"Total EUR: {orders.Sum(o => o.Amount * o.Price):F2}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
        }

        await Task.CompletedTask;
    }
}