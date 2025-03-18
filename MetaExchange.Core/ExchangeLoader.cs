using System.Text.Json;
using MetaExchange.Core.Models;

namespace MetaExchange.Core;

public class ExchangeLoader(string exchangesFolderPath) : IExchangeLoader
{
    private readonly string _exchangesFolderPath = exchangesFolderPath ?? throw new ArgumentNullException(nameof(exchangesFolderPath));

    public List<(string Name, ExchangeData Data)> LoadExchanges()
    {
        if (!Directory.Exists(_exchangesFolderPath))
            throw new DirectoryNotFoundException($"Exchanges folder not found at: {_exchangesFolderPath}");

        var exchanges = new List<(string Name, ExchangeData)>();
        var files = Directory.GetFiles(_exchangesFolderPath, "exchange-*.json");
        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var json = File.ReadAllText(file);
            var data = JsonSerializer.Deserialize<ExchangeData>(json);
            exchanges.Add((fileName, data)!);
        }
        return exchanges;
    }
}