using MetaExchange.Core.Models;
using Xunit;

namespace MetaExchange.Tests;

public class MetaExchangeTests
{
    private readonly Core.MetaExchange _metaExchange = new();

    [Fact]
    public void BuyOrder_SimpleCase_ReturnsBestPrice()
    {
        var exchanges = CreateExchanges(
            ("ex1", new AvailableFunds { Euro = 20000m, Crypto = 3m },
                [(3000m, 5m)], null),
            ("ex2", new AvailableFunds { Euro = 20000m, Crypto = 9m },
                [(3100m, 5m)], null)
        );

        var request = new OrderRequest { Type = OrderType.Buy, Amount = 3m };
        var result = _metaExchange.ProcessOrder(exchanges, request);

        Assert.Single(result);
        Assert.Equal("ex1", result[0].Exchange);
        Assert.Equal(3000m, result[0].Price);
        Assert.Equal(3m, result[0].Amount);
    }

    [Fact]
    public void BuyOrder_MultipleExchanges_ReturnsCheapestCombination()
    {
        var exchanges = CreateExchanges(
            ("ex1", new AvailableFunds { Euro = 10000m, Crypto = 1m },
                [(3000m, 2m)], null),
            ("ex2", new AvailableFunds { Euro = 20000m, Crypto = 3m },
                [(3100m, 5m)], null),
            ("ex3", new AvailableFunds { Euro = 22000m, Crypto = 4m },
                [(3200m, 5m)], null)
        );

        var request = new OrderRequest { Type = OrderType.Buy, Amount = 5m };
        var result = _metaExchange.ProcessOrder(exchanges, request);

        Assert.Equal(2, result.Count);
        Assert.Equal("ex1", result[0].Exchange);
        Assert.Equal(2, result[0].Amount);
        Assert.Equal("ex2", result[1].Exchange);
        Assert.Equal(3, result[1].Amount);
    }

    [Fact]
    public void BuyOrder_InsufficientLiquidity_ThrowsException()
    {
        var exchanges = CreateExchanges(
            ("ex1", new AvailableFunds { Euro = 0m, Crypto = 500m },
                [(3000m, 1m)], null),
            ("ex2", new AvailableFunds { Euro = 0m, Crypto = 500m },
                [(3100m, 1m)], null)
        );

        var request = new OrderRequest { Type = OrderType.Buy, Amount = 3m };
        var exception = Assert.Throws<Exception>(() => _metaExchange.ProcessOrder(exchanges, request));
        Assert.Equal("Insufficient liquidity to buy 3 BTC at optimal prices", exception.Message);
    }

    [Fact]
    public void BuyOrder_NoBids_ThrowsException()
    {
        var exchanges = CreateExchanges(
            ("ex1", new AvailableFunds { Euro = 50000m, Crypto = 5m }, null, []),
            ("ex2", new AvailableFunds { Euro = 50000m, Crypto = 5m }, null, [])
        );

        var request = new OrderRequest { Type = OrderType.Buy, Amount = 3m };
        var exception = Assert.Throws<Exception>(() => _metaExchange.ProcessOrder(exchanges, request));
        Assert.Equal("Insufficient liquidity to buy 3 BTC at optimal prices", exception.Message);
    }

    [Fact]
    public void SellOrder_SimpleCase_ReturnsHighestPrice()
    {
        var exchanges = CreateExchanges(
            ("ex1", new AvailableFunds { Euro = 50000m, Crypto = 5m },
                null, [(3000m, 5m)]),
            ("ex2", new AvailableFunds { Euro = 50000m, Crypto = 5m },
                null, [(2900m, 5m)])
        );

        var request = new OrderRequest { Type = OrderType.Sell, Amount = 3m };
        var result = _metaExchange.ProcessOrder(exchanges, request);

        Assert.Single(result);
        Assert.Equal("ex1", result[0].Exchange);
        Assert.Equal(3000m, result[0].Price);
        Assert.Equal(3m, result[0].Amount);
    }

    [Fact]
    public void SellOrder_MultipleExchanges_ReturnsHighestCombination()
    {
        var exchanges = CreateExchanges(
            ("ex1", new AvailableFunds { Euro = 50000m, Crypto = 2m },
                null, [(3100m, 2m)]),
            ("ex2", new AvailableFunds { Euro = 50000m, Crypto = 5m },
                null, [(3000m, 5m)]),
            ("ex3", new AvailableFunds { Euro = 50000m, Crypto = 5m },
                null, [(2900m, 5m)])
        );

        var request = new OrderRequest { Type = OrderType.Sell, Amount = 5m };
        var result = _metaExchange.ProcessOrder(exchanges, request);

        Assert.Equal(2, result.Count);
        Assert.Equal("ex1", result[0].Exchange);
        Assert.Equal(3100m, result[0].Price);
        Assert.Equal(2m, result[0].Amount);
        Assert.Equal("ex2", result[1].Exchange);
        Assert.Equal(3000m, result[1].Price);
        Assert.Equal(3m, result[1].Amount);
    }

    [Fact]
    public void SellOrder_InsufficientBtcBalance_ThrowsException()
    {
        var exchanges = CreateExchanges(
            ("ex1", new AvailableFunds { Euro = 50000m, Crypto = 1m },
                null, [(3100m, 2m)]),
            ("ex2", new AvailableFunds { Euro = 50000m, Crypto = 1m },
                null, [(3000m, 5m)])
        );

        var request = new OrderRequest { Type = OrderType.Sell, Amount = 3m };
        var exception = Assert.Throws<Exception>(() => _metaExchange.ProcessOrder(exchanges, request));
        Assert.Equal("Insufficient liquidity to sell 1 BTC at optimal prices", exception.Message);
    }

    [Fact]
    public void SellOrder_NoBids_ThrowsException()
    {
        var exchanges = CreateExchanges(
            ("ex1", new AvailableFunds { Euro = 50000m, Crypto = 5m }, null, []),
            ("ex2", new AvailableFunds { Euro = 50000m, Crypto = 5m }, null, [])
        );

        var request = new OrderRequest { Type = OrderType.Sell, Amount = 3m };
        var exception = Assert.Throws<Exception>(() => _metaExchange.ProcessOrder(exchanges, request));
        Assert.Equal("Insufficient liquidity to sell 3 BTC at optimal prices", exception.Message);
    }

    // Helper method to create exchanges with asks and/or bids
    private List<(string Name, ExchangeData Data)> CreateExchanges(
        params (string Name, AvailableFunds Funds, List<(decimal Price, decimal Amount)>? Asks,
            List<(decimal Price, decimal Amount)>? Bids)[] exchangeData)
    {
        var exchanges = new List<(string, ExchangeData)>();
        foreach (var (name, funds, asks, bids) in exchangeData)
        {
            var exchange = new ExchangeData
            {
                Id = name,
                AvailableFunds = funds,
                OrderBook = new OrderBook
                {
                    Asks = asks != null
                        ? asks.Select(a => new OrderWrapper { Order = new Order { Price = a.Price, Amount = a.Amount, Type = OrderType.Sell } }).ToList()
                        : [],
                    Bids = bids != null
                        ? bids.Select(b => new OrderWrapper { Order = new Order { Price = b.Price, Amount = b.Amount, Type = OrderType.Buy } }).ToList()
                        : []
                }
            };
            exchanges.Add((name, exchange));
        }

        return exchanges;
    }
}
