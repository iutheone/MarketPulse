using System.Text.Json;
using MarketPulse.Application.Interfaces;
using MarketPulse.Domain.Anomaly;
using StackExchange.Redis;

namespace MarketPulse.Infrastructure.Redis;

public sealed class RedisAnomalyFeed : IAnomalyFeed
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IConnectionMultiplexer _redis;

    public RedisAnomalyFeed(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task PushAsync(AnomalyResult anomaly, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(anomaly, Json);
        var batch = db.CreateBatch();
        var latest = batch.ListLeftPushAsync(LatestListKey, json);
        var trim = batch.ListTrimAsync(LatestListKey, 0, 99);
        var symbol = batch.StringSetAsync(SymbolLatestKey(anomaly.Symbol), json);
        batch.Execute();
        await Task.WhenAll(latest, trim, symbol);
    }

    internal const string LatestListKey = "market:anomalies:latest";
    internal static string SymbolLatestKey(string symbol) => $"market:{symbol}:anomaly:latest";
}
