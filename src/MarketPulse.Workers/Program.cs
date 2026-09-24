using MarketPulse.Infrastructure;
using MarketPulse.Workers.FeatureProcessor;
using MarketPulse.Workers.MarketIngestion;
using MarketPulse.Workers.Replay;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddMarketPulseInfrastructure(builder.Configuration);

var provider = builder.Configuration["MarketData:Provider"] ?? "Synthetic";
if (provider.Equals("Replay", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHostedService<HistoricalReplayWorker>();
}
else
{
    builder.Services.AddHostedService<MarketDataIngestionWorker>();
}

builder.Services.AddHostedService<FeatureProcessorWorker>();

var host = builder.Build();
await host.Services.InitializeMarketPulseDatabaseAsync();
await host.RunAsync();
