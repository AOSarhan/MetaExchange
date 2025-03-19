using MetaExchange.Core;
using Microsoft.Extensions.Configuration;
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
                services.AddSingleton<IExchangeLoader, ExchangeLoader>();
                services.AddSingleton<IMetaExchange, Core.MetaExchange>();
                services.AddTransient<App>();
            });
    }
}