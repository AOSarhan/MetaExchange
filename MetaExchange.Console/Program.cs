using MetaExchange.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MetaExchange.Console;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        var app = host.Services.GetRequiredService<App>();
        await app.RunAsync(args);
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                //var basePath = Path.GetFullPath(Path.Combine(
                //    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory(),
                //    "..", "..", "..", ".."));
                //var exchangesPath = Path.Combine(basePath, "exchanges");

                var exchangesPath = "C:\\Users\\ahsarhan\\source\\repos\\MetaExchange\\exchanges";
                if (string.IsNullOrEmpty(exchangesPath) || !Directory.Exists(exchangesPath))
                {
                    throw new InvalidOperationException($"Exchanges path '{exchangesPath}' is invalid or does not exist.");
                }

                services.AddSingleton<IExchangeLoader>(new ExchangeLoader(exchangesPath));
                services.AddSingleton<IMetaExchange, Core.MetaExchange>();
                services.AddTransient<App>();
            });
    }
}