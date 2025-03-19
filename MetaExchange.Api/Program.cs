using MetaExchange.Core;

namespace MetaExchange.Api;

public sealed class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSingleton<IExchangeLoader, ExchangeLoader>();
            builder.Services.AddSingleton<IMetaExchange, Core.MetaExchange>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            await app.RunAsync();
            return 0;
        }
        catch (Exception e)
        {
            System.Console.WriteLine(e);
            throw;
        }
    }
}