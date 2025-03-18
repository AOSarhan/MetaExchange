using MetaExchange.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var exchangesPath = Path.Combine(Directory.GetCurrentDirectory(), "exchanges");
builder.Services.AddSingleton<IExchangeLoader>(new ExchangeLoader(exchangesPath));
builder.Services.AddSingleton<IMetaExchange, MetaExchange.Core.MetaExchange>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();