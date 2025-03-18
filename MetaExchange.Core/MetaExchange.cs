using MetaExchange.Core.Models;

namespace MetaExchange.Core;

public class MetaExchange : IMetaExchange
{
    public List<ExecutionOrder> ProcessOrder(List<(string Name, ExchangeData Data)> exchanges, OrderRequest request)
    {
        return request.Type switch
        {
            OrderType.Sell => ProcessSellOrder(exchanges, request.Amount),
            OrderType.Buy => ProcessBuyOrder(exchanges, request.Amount),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Type), "Invalid order type")
        };
    }

    private List<ExecutionOrder> ProcessSellOrder(List<(string Name, ExchangeData Data)> exchanges, decimal amount)
    {
        var result = new List<ExecutionOrder>();
        var remainingAmount = amount;

        // Get all bids across exchanges (sorted by highest price first)
        var allBids = exchanges
            .SelectMany(e => e.Data.OrderBook.Bids.Select(b => new
            {
                Exchange = e.Name,
                Bid = b.Order,
                MaxEurAvailable = e.Data.AvailableFunds.EurBalance
            }))
            .Where(x => x.Bid.Type == OrderType.Buy)
            .OrderByDescending(x => x.Bid.Price)
            .ToList();

        foreach (var bid in allBids)
        {
            if (remainingAmount <= 0) break;

            var btcToSell = Math.Min(remainingAmount, bid.Bid.Amount);
            var maxEurCanPay = bid.MaxEurAvailable;
            var maxBtcCanBuy = maxEurCanPay / bid.Bid.Price;

            btcToSell = Math.Min(btcToSell, maxBtcCanBuy); // Ensure the buyer has enough EUR

            if (btcToSell > 0)
            {
                result.Add(new ExecutionOrder
                {
                    Exchange = bid.Exchange,
                    Amount = btcToSell,
                    Price = bid.Bid.Price
                });

                remainingAmount -= btcToSell;
            }
        }

        if (remainingAmount > 0)
            throw new Exception($"Insufficient liquidity to sell {remainingAmount} BTC at optimal prices");

        return result;
    }

    private List<ExecutionOrder> ProcessBuyOrder(List<(string Name, ExchangeData Data)> exchanges, decimal amount)
    {
        var result = new List<ExecutionOrder>();
        var remainingAmount = amount;

        // Get all asks across exchanges (sorted by lowest price first)
        var allAsks = exchanges
            .SelectMany(e => e.Data.OrderBook.Asks.Select(a => new
            {
                Exchange = e.Name,
                Ask = a.Order,
                MaxBtcAvailable = e.Data.AvailableFunds.BtcBalance,
                MaxEurAvailable = e.Data.AvailableFunds.EurBalance
            }))
            .Where(x => x.Ask.Type == OrderType.Sell)
            .OrderBy(x => x.Ask.Price)
            .ToList();

        foreach (var ask in allAsks)
        {
            if (remainingAmount <= 0) break;

            var btcToBuy = Math.Min(remainingAmount, ask.Ask.Amount);
            var eurNeeded = btcToBuy * ask.Ask.Price;

            // Ensure enough BTC is available on the exchange
            btcToBuy = Math.Min(btcToBuy, ask.MaxBtcAvailable);

            // Ensure we have enough EUR to buy
            if (eurNeeded > ask.MaxEurAvailable)
                btcToBuy = ask.MaxEurAvailable / ask.Ask.Price;

            if (btcToBuy > 0)
            {
                result.Add(new ExecutionOrder
                {
                    Exchange = ask.Exchange,
                    Amount = btcToBuy,
                    Price = ask.Ask.Price
                });

                remainingAmount -= btcToBuy;
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

        // Use PriorityQueue to efficiently manage orders by price
        var queue = new PriorityQueue<(string Exchange, Order Order, decimal MaxBtc, decimal MaxEur), decimal>(
            isBuy ? Comparer<decimal>.Create((a, b) => a.CompareTo(b)) : Comparer<decimal>.Create((a, b) => b.CompareTo(a)));

        // Populate the queue with all relevant orders
        foreach (var (name, data) in exchanges)
        {
            var orders = isBuy ? data.OrderBook.Asks : data.OrderBook.Bids;
            foreach (var wrapper in orders)
            {
                var order = wrapper.Order;
                if ((isBuy && order.Type == OrderType.Sell) || (!isBuy && order.Type == OrderType.Buy))
                {
                    queue.Enqueue(
                        (name, order, data.AvailableFunds.BtcBalance, data.AvailableFunds.EurBalance),
                        order.Price
                    );
                }
            }
        }

        // Process orders from the queue
        while (queue.Count > 0 && remainingAmount > 0)
        {
            var (exchange, order, maxBtcAvailable, maxEurAvailable) = queue.Dequeue();

            var amountToTrade = Math.Min(remainingAmount, order.Amount);
            var eurNeeded = amountToTrade * order.Price;

            if (isBuy)
            {
                amountToTrade = Math.Min(amountToTrade, maxBtcAvailable); // Seller's BTC limit
                if (eurNeeded > maxEurAvailable && amountToTrade > 0)
                    amountToTrade = decimal.Round(maxEurAvailable / order.Price, 8, MidpointRounding.ToZero); // Buyer's EUR limit
            }
            else
            {
                var maxBtcCanBuy = maxEurAvailable / order.Price;
                amountToTrade = Math.Min(amountToTrade, maxBtcCanBuy); // Buyer's EUR limit
                amountToTrade = Math.Min(amountToTrade, maxBtcAvailable); // Seller's BTC limit
            }

            amountToTrade = decimal.Round(amountToTrade, 8, MidpointRounding.ToZero);

            if (amountToTrade > 0)
            {
                result.Add(new ExecutionOrder
                {
                    Exchange = exchange,
                    Amount = amountToTrade,
                    Price = order.Price
                });
                remainingAmount -= amountToTrade;
            }
        }

        if (remainingAmount > 0)
            throw new Exception($"Insufficient liquidity to {request.Type.ToString().ToLower()} {remainingAmount} BTC at optimal prices");

        return result;
    }
}