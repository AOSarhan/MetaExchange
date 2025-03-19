using System.Reflection;
using System.Text.Json;
using MetaExchange.Core.Models;
using Microsoft.Extensions.Configuration;

namespace MetaExchange.Core;

public class ExchangeLoader : IExchangeLoader
{
    private readonly string _exchangesFolderPath;

    public ExchangeLoader()
    {
        // This is some primitive loading, can be replaced with a more advanced
        // one depending on the location of the exchange files 
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyDir = Path.GetDirectoryName(assembly.Location)
                          ?? throw new InvalidOperationException("Could not determine assembly directory.");

        var configPath = Path.Combine(assemblyDir, "appsettings.json");
        if (!File.Exists(configPath))
            throw new FileNotFoundException($"appsettings.json not found at: {configPath}");

        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: false)
            .Build();

        var settings = configBuilder.GetSection("ExchangeSettings").Get<ExchangeSettings>()
                       ?? throw new InvalidOperationException("ExchangeSettings section not found in configuration.");

        _exchangesFolderPath = settings.ExchangesFolderPath;

        if (string.IsNullOrEmpty(_exchangesFolderPath) || !Directory.Exists(_exchangesFolderPath))
            throw new DirectoryNotFoundException($"Exchanges folder not found at: {_exchangesFolderPath}");
    }

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