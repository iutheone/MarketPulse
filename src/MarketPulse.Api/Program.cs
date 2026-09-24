using MarketPulse.Api.Hubs;
using MarketPulse.Api.Realtime;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Options;
using MarketPulse.Infrastructure;
using MarketPulse.Infrastructure.Kafka;
using MarketPulse.Infrastructure.Observability;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddMarketPulseInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddCheck<KafkaClusterHealthCheck>("kafka", tags: ["ready"])
    .AddCheck<RedisHealthCheck>("redis", tags: ["ready"])
    .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);

var signalR = builder.Configuration.GetSection(SignalROptions.SectionName).Get<SignalROptions>() ?? new SignalROptions();
var signalRBuilder = builder.Services.AddSignalR();
if (signalR.UseRedisBackplane)
{
    var redis = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
    signalRBuilder.AddStackExchangeRedis(redis.ConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("marketpulse-signalr");
    });
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins(signalR.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSingleton<IAnomalyRealtimePublisher, SignalRAnomalyPublisher>();
builder.Services.AddSingleton<IAnomalyRealtimeConsumer, KafkaAnomalyRealtimeConsumer>();
builder.Services.AddHostedService<AnomalyRealtimeWorker>();

var app = builder.Build();
await app.Services.InitializeMarketPulseDatabaseAsync();

app.UseExceptionHandler();
app.UseCors("frontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Ok(new
{
    service = "MarketPulse.Api",
    phase = 7,
    note = "Anomaly scores describe unusual activity. They are not buy/sell advice.",
    hub = AnomalyHub.Path
}));

app.MapControllers();
app.MapHub<AnomalyHub>(AnomalyHub.Path);

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
