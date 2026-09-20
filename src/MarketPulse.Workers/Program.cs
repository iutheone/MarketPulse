using MarketPulse.Infrastructure;
using MarketPulse.Workers.FeatureProcessor;
using MarketPulse.Workers.MarketIngestion;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddMarketPulseInfrastructure(builder.Configuration);
builder.Services.AddHostedService<MarketDataIngestionWorker>();
builder.Services.AddHostedService<FeatureProcessorWorker>();

var host = builder.Build();
await host.RunAsync();
