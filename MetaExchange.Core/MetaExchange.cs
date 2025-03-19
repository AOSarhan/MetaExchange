using MetaExchange.Core.Models;

namespace MetaExchange.Core;

public class MetaExchange : IMetaExchange
{
    public List<ExecutionOrder> ProcessOrder(List<(string Name, ExchangeData Data)> exchanges, OrderRequest request)
    {
        if (request.Amount <= 0) throw new ArgumentException("Amount must be positive.");
        return request.Type switch
        {
            OrderType.Sell => ProcessSellOrder(exchanges, request.Amount),
            OrderType.Buy => ProcessBuyOrder(exchanges, request.Amount),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Type), "Invalid order type")
        };
    }

    private static List<ExecutionOrder> ProcessSellOrder(List<(string Name, ExchangeData Data)> exchanges, decimal amount)
    {
        var result = new List<ExecutionOrder>();
        var remainingAmount = amount;
        var btcSoldByExchange = new Dictionary<string, decimal>();

        var allBids = exchanges
            .SelectMany(e => e.Data.OrderBook.Bids.Select(b => new
            {
                Exchange = e.Name,
                Bid = b.Order,
                MaxBtcAvailable = e.Data.AvailableFunds.Crypto
            }))
            .Where(x => x.Bid.Type == OrderType.Buy)
            .OrderByDescending(x => x.Bid.Price)
            .ToList();

        foreach (var bid in allBids)
        {
            if (remainingAmount <= 0) break;

            btcSoldByExchange.TryAdd(bid.Exchange, 0);

            // check remaining BTC available to sell for this exchange
            var btcAvailable = bid.MaxBtcAvailable - btcSoldByExchange[bid.Exchange];
            if (btcAvailable <= 0) continue; // Skip if no BTC left to sell

            // check if the amount we want to sell is less than the remaining and the available amount
            var btcToSell = Math.Min(remainingAmount, bid.Bid.Amount);
            btcToSell = Math.Min(btcToSell, btcAvailable);

            if (btcToSell > 0)
            {
                result.Add(new ExecutionOrder
                {
                    Exchange = bid.Exchange,
                    Amount = btcToSell,
                    Price = bid.Bid.Price
                });

                remainingAmount -= btcToSell;
                btcSoldByExchange[bid.Exchange] += btcToSell;
            }
        }

        if (remainingAmount > 0)
            throw new Exception($"Insufficient liquidity to sell {remainingAmount} BTC at optimal prices");

        return result;
    }

    private static List<ExecutionOrder> ProcessBuyOrder(List<(string Name, ExchangeData Data)> exchanges, decimal amount)
    {
        var result = new List<ExecutionOrder>();
        var remainingAmount = amount;
        var eurSpentByExchange = new Dictionary<string, decimal>();

        var allAsks = exchanges
            .SelectMany(e => e.Data.OrderBook.Asks.Select(a => new
            {
                Exchange = e.Name,
                Ask = a.Order,
                MaxEurAvailable = e.Data.AvailableFunds.Euro
            }))
            .Where(x => x.Ask.Type == OrderType.Sell)
            .OrderBy(x => x.Ask.Price)
            .ToList();

        foreach (var ask in allAsks)
        {
            if (remainingAmount <= 0) break;

            eurSpentByExchange.TryAdd(ask.Exchange, 0);

            // calculate remaining EUR available to spend for this exchange
            var eurAvailable = ask.MaxEurAvailable - eurSpentByExchange[ask.Exchange];
            if (eurAvailable <= 0) continue;

            var btcToBuy = Math.Min(remainingAmount, ask.Ask.Amount);

            // check if we have enough EUR to buy the amount of BTC we want
            var eurNeeded = btcToBuy * ask.Ask.Price;
            if (eurNeeded > eurAvailable)
                btcToBuy = eurAvailable / ask.Ask.Price;

            if (btcToBuy > 0)
            {
                result.Add(new ExecutionOrder
                {
                    Exchange = ask.Exchange,
                    Amount = btcToBuy,
                    Price = ask.Ask.Price
                });

                remainingAmount -= btcToBuy;
                eurSpentByExchange[ask.Exchange] += btcToBuy * ask.Ask.Price;
            }
        }

        if (remainingAmount > 0)
            throw new Exception($"Insufficient liquidity to buy {remainingAmount} BTC at optimal prices");

        return result;
    }


    // Large data set implementation
    // instead of sorting all orders across all exchanges (O(nlogn)) we use the PriorityQueue to enqueue (O(nlogn)) and dequeue (O(logn))
    // downside is that PriorityQueue is not thread safe
    public List<ExecutionOrder> ProcessOrderLargeSet(List<(string Name, ExchangeData Data)> exchanges, OrderRequest request)
    {
        if (exchanges == null || !exchanges.Any()) throw new ArgumentException("No exchanges provided.");
        if (request.Amount <= 0) throw new ArgumentException("Amount must be positive.");

        var isBuy = request.Type == OrderType.Buy;
        var remainingAmount = request.Amount;
        var result = new List<ExecutionOrder>();

        // Track funds per exchange: EUR spent for buy, BTC sold for sell
        var fundsUsedByExchange = new Dictionary<string, decimal>();

        // Use PriorityQueue to manage orders by price: min-heap for buy (lowest first), max-heap for sell (highest first)
        var queue = new PriorityQueue<(string Exchange, Order Order, decimal MaxBtc, decimal MaxEur), decimal>(
            isBuy ? Comparer<decimal>.Create((a, b) => a.CompareTo(b)) : Comparer<decimal>.Create((a, b) => b.CompareTo(a)));

        // Populate the queue with all relevant orders
        foreach (var (name, data) in exchanges)
        {
            var orders = isBuy ? data.OrderBook.Asks : data.OrderBook.Bids;
            fundsUsedByExchange.TryAdd(name, 0); // Initialize funds used for this exchange
            foreach (var wrapper in orders)
            {
                var order = wrapper.Order;
                if ((isBuy && order.Type == OrderType.Sell) || (!isBuy && order.Type == OrderType.Buy))
                {
                    queue.Enqueue(
                        (name, order, data.AvailableFunds.Crypto, data.AvailableFunds.Euro),
                        order.Price
                    );
                }
            }
        }

        // Process orders from the queue
        while (queue.Count > 0 && remainingAmount > 0)
        {
            var (exchange, order, maxBtcAvailable, maxEurAvailable) = queue.Dequeue();

            // Calculate available funds for this exchange
            var fundsAvailable = isBuy
                ? maxEurAvailable - fundsUsedByExchange[exchange] // Remaining EUR for buy
                : maxBtcAvailable - fundsUsedByExchange[exchange]; // Remaining BTC for sell

            if (fundsAvailable <= 0) continue; // Skip if no funds left for this exchange

            // Determine amount to trade
            var amountToTrade = Math.Min(remainingAmount, order.Amount);

            if (isBuy)
            {
                // Limit by buyer's EUR availability
                var eurNeeded = amountToTrade * order.Price;
                if (eurNeeded > fundsAvailable)
                    amountToTrade = fundsAvailable / order.Price;
            }
            else
            {
                // Limit by seller's BTC availability
                amountToTrade = Math.Min(amountToTrade, fundsAvailable);
            }

            if (amountToTrade > 0)
            {
                result.Add(new ExecutionOrder
                {
                    Exchange = exchange,
                    Amount = amountToTrade,
                    Price = order.Price
                });
                remainingAmount -= amountToTrade;
                fundsUsedByExchange[exchange] += isBuy ? amountToTrade * order.Price : amountToTrade;
                if (remainingAmount <= 0) break;
            }
        }

        if (remainingAmount > 0)
            throw new Exception($"Insufficient liquidity to {request.Type.ToString().ToLower()} {remainingAmount} BTC at optimal prices");

        return result;
    }
}