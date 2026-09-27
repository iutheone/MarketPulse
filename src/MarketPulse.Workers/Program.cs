using MarketPulse.Infrastructure;
using MarketPulse.Infrastructure.Observability;
using MarketPulse.Workers.Alerts;
using MarketPulse.Workers.FeatureProcessor;
using MarketPulse.Workers.MarketIngestion;
using MarketPulse.Workers.Replay;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "O";
});

builder.Services.AddMarketPulseInfrastructure(builder.Configuration);
builder.Services.AddMarketPulseWorkerTelemetry(builder.Configuration);

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
builder.Services.AddHostedService<AlertDispatcherWorker>();
builder.Services.AddHostedService<AlertDeliveryWorker>();

var host = builder.Build();
await host.Services.InitializeMarketPulseDatabaseAsync();
await host.RunAsync();
