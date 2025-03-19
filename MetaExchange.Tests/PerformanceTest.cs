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
            var exchanges = GenerateLargeDataset(100000, 1000);
            var request = new OrderRequest { Type = OrderType.Buy, Amount = 30m };
            var metaExchange = new Core.MetaExchange();

            var stopwatch = Stopwatch.StartNew();
            metaExchange.ProcessOrderLargeSet(exchanges, request);
            stopwatch.Stop();

            var stopwatch2 = Stopwatch.StartNew();
            metaExchange.ProcessOrder(exchanges, request);
            stopwatch2.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds < stopwatch2.ElapsedMilliseconds);
        }

        private List<(string, ExchangeData)> GenerateLargeDataset(int exchangeCount, int ordersPerExchange)
        {
            var random = new Random();
            var result = new List<(string, ExchangeData)>();
            for (var i = 0; i < exchangeCount; i++)
            {
                var asks = new List<OrderWrapper>();
                for (var j = 0; j < ordersPerExchange; j++)
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
                    AvailableFunds = new AvailableFunds { Crypto = 1000, Euro = 1000000 },
                    OrderBook = new OrderBook { Asks = asks, Bids = [] }
                }));
            }
            return result;
        }
    }
}
