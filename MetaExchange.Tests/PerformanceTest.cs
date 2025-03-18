using MetaExchange.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MetaExchange.Tests
{
    public class PerformanceTest
    {
        [Fact]
        public void ComparePerformance()
        {
            var exchanges = GenerateLargeDataset(100000, 1000); // 100 exchanges, 100 orders each
            var request = new OrderRequest { Type = OrderType.Buy, Amount = 1000m };
            var metaExchange = new Core.MetaExchange();

            var stopwatch = Stopwatch.StartNew();
            var result = metaExchange.ProcessOrder(exchanges, request);
            stopwatch.Stop();

            Console.WriteLine($"Processed {result.Count} orders in {stopwatch.ElapsedMilliseconds} ms");
        }

        private List<(string, ExchangeData)> GenerateLargeDataset(int exchangeCount, int ordersPerExchange)
        {
            var random = new Random();
            var result = new List<(string, ExchangeData)>();
            for (int i = 0; i < exchangeCount; i++)
            {
                var asks = new List<OrderWrapper>();
                for (int j = 0; j < ordersPerExchange; j++)
                {
                    asks.Add(new OrderWrapper
                    {
                        Order = new Order
                        {
                            Type = OrderType.Sell,
                            Amount = (decimal)random.NextDouble() * 10,
                            Price = 50000 + random.Next(0, 1000)
                        }
                    });
                }
                result.Add(($"exchange-{i}", new ExchangeData
                {
                    Id = $"exchange-{i}",
                    AvailableFunds = new AvailableFunds { BtcBalance = 1000, EurBalance = 1000000 },
                    OrderBook = new OrderBook { Asks = asks, Bids = new List<OrderWrapper>() }
                }));
            }
            return result;
        }
    }
}
