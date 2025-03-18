using MetaExchange.Core.Models;

namespace MetaExchange.Core;

public interface IExchangeLoader
{
    List<(string Name, ExchangeData Data)> LoadExchanges();
}