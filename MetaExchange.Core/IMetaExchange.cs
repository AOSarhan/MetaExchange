using MetaExchange.Core.Models;

namespace MetaExchange.Core;

public interface IMetaExchange
{
    List<ExecutionOrder> ProcessOrder(List<(string Name, ExchangeData Data)> exchanges, OrderRequest request);
    List<ExecutionOrder> ProcessOrderLargeSet(List<(string Name, ExchangeData Data)> exchanges, OrderRequest request);
}