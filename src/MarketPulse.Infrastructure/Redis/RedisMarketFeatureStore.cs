using System.Globalization;
using System.Text.Json;
using MarketPulse.Application.Interfaces;
using MarketPulse.Domain.Events;
using MarketPulse.Domain.Features;
using StackExchange.Redis;

namespace MarketPulse.Infrastructure.Redis;

/// <summary>
/// Redis is the hot cache for rolling windows. It is not durable history — a flush loses features
/// until Kafka is replayed. Sorted sets expire old ticks by score (event time).
/// </summary>
public sealed class RedisMarketFeatureStore : IMarketFeatureStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IConnectionMultiplexer _redis;

    public RedisMarketFeatureStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task AppendAsync(MarketTick tick, TimeSpan retention, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var windowKey = WindowKey(tick.Symbol);
        var score = tick.Timestamp.ToUnixTimeMilliseconds();
        var member = FormatMember(tick);
        var cutoff = tick.Timestamp.Subtract(retention).ToUnixTimeMilliseconds();

        var batch = db.CreateBatch();
        var zadd = batch.SortedSetAddAsync(windowKey, member, score);
        var trim = batch.SortedSetRemoveRangeByScoreAsync(windowKey, double.NegativeInfinity, cutoff);
        var latest = batch.StringSetAsync(LatestKey(tick.Symbol), JsonSerializer.Serialize(tick, Json));
        batch.Execute();

        await Task.WhenAll(zadd, trim, latest);
    }

    public async Task<IReadOnlyList<TickSample>> GetWindowAsync(
        string symbol,
        DateTimeOffset fromInclusive,
        CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var entries = await db.SortedSetRangeByScoreAsync(
            WindowKey(symbol),
            fromInclusive.ToUnixTimeMilliseconds(),
            double.PositiveInfinity);

        var samples = new List<TickSample>(entries.Length);
        foreach (var entry in entries)
        {
            if (TryParseMember(entry!, out var sample))
            {
                samples.Add(sample);
            }
        }

        return samples;
    }

    public async Task SaveFeaturesAsync(MarketFeatures features, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(features, Json);
        var batch = db.CreateBatch();
        var f = batch.StringSetAsync(FeaturesKey(features.Symbol), json);
        var v1 = batch.StringSetAsync(VolumeKey(features.Symbol, "1m"), features.Volume1m.ToString(CultureInfo.InvariantCulture));
        var v5 = batch.StringSetAsync(VolumeKey(features.Symbol, "5m"), features.Volume5m.ToString(CultureInfo.InvariantCulture));
        var v15 = batch.StringSetAsync(VolumeKey(features.Symbol, "15m"), features.Volume15m.ToString(CultureInfo.InvariantCulture));
        var vwap = batch.HashSetAsync(VwapKey(features.Symbol),
        [
            new HashEntry("value", features.Vwap.ToString(CultureInfo.InvariantCulture)),
            new HashEntry("deviationPct", features.VwapDeviationPercent.ToString(CultureInfo.InvariantCulture)),
            new HashEntry("eventId", features.SourceEventId.ToString())
        ]);
        batch.Execute();
        await Task.WhenAll(f, v1, v5, v15, vwap);
    }

    public async Task<MarketFeatures?> GetFeaturesAsync(string symbol, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var json = await db.StringGetAsync(FeaturesKey(symbol));
        if (json.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<MarketFeatures>(json!, Json);
    }

    public async Task<MarketTick?> GetLatestTickAsync(string symbol, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var json = await db.StringGetAsync(LatestKey(symbol));
        if (json.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<MarketTick>(json!, Json);
    }

    internal static string LatestKey(string symbol) => $"market:{symbol}:latest";
    internal static string WindowKey(string symbol) => $"market:{symbol}:window";
    internal static string FeaturesKey(string symbol) => $"market:{symbol}:features";
    internal static string VolumeKey(string symbol, string window) => $"market:{symbol}:volume:{window}";
    internal static string VwapKey(string symbol) => $"market:{symbol}:vwap";

    internal static string FormatMember(MarketTick tick)
    {
        return string.Join('|',
            tick.EventId.ToString("N"),
            tick.Timestamp.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            tick.Open.ToString(CultureInfo.InvariantCulture),
            tick.High.ToString(CultureInfo.InvariantCulture),
            tick.Low.ToString(CultureInfo.InvariantCulture),
            tick.Close.ToString(CultureInfo.InvariantCulture),
            tick.Volume.ToString(CultureInfo.InvariantCulture));
    }

    internal static bool TryParseMember(string member, out TickSample sample)
    {
        sample = null!;
        var parts = member.Split('|');
        if (parts.Length != 7)
        {
            return false;
        }

        if (!Guid.TryParseExact(parts[0], "N", out var eventId))
        {
            return false;
        }

        if (!long.TryParse(parts[1], CultureInfo.InvariantCulture, out var unixMs))
        {
            return false;
        }

        sample = new TickSample
        {
            EventId = eventId,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(unixMs),
            Open = decimal.Parse(parts[2], CultureInfo.InvariantCulture),
            High = decimal.Parse(parts[3], CultureInfo.InvariantCulture),
            Low = decimal.Parse(parts[4], CultureInfo.InvariantCulture),
            Close = decimal.Parse(parts[5], CultureInfo.InvariantCulture),
            Volume = long.Parse(parts[6], CultureInfo.InvariantCulture)
        };
        return true;
    }
}
