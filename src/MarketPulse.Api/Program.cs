using MarketPulse.Infrastructure;
using MarketPulse.Infrastructure.Observability;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddMarketPulseInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddCheck<KafkaClusterHealthCheck>("kafka", tags: ["ready"])
    .AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Ok(new
{
    service = "MarketPulse.Api",
    phase = 2,
    note = "Health plus a Redis feature snapshot. Anomaly REST arrives in Phase 4."
}));

app.MapGet("/api/v1/stocks/{symbol}/features", async (
    string symbol,
    MarketPulse.Application.Interfaces.IMarketFeatureStore store,
    CancellationToken cancellationToken) =>
{
    var features = await store.GetFeaturesAsync(symbol.ToUpperInvariant(), cancellationToken);
    return features is null ? Results.NotFound() : Results.Ok(features);
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
