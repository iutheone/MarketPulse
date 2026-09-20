namespace MarketPulse.Domain.Features;

/// <summary>
/// Pure feature math over event-time samples. Callers must pass only ticks at or before the current event.
/// </summary>
public sealed class FeatureCalculator
{
    public const int CurrentWindowMinutes = 1;
    public const int MediumWindowMinutes = 5;
    public const int LongWindowMinutes = 15;

    public MarketFeatures Calculate(string symbol, IReadOnlyList<TickSample> oldestFirst)
    {
        if (oldestFirst.Count == 0)
        {
            throw new ArgumentException("At least one tick is required.", nameof(oldestFirst));
        }

        var current = oldestFirst[^1];
        var now = current.Timestamp;
        var previous = oldestFirst.Count > 1 ? oldestFirst[^2] : current;

        var volume1m = SumVolume(oldestFirst, now.AddMinutes(-CurrentWindowMinutes), now);
        var volume5m = SumVolume(oldestFirst, now.AddMinutes(-MediumWindowMinutes), now);
        var volume15m = SumVolume(oldestFirst, now.AddMinutes(-LongWindowMinutes), now);
        var previous1m = SumVolume(
            oldestFirst,
            now.AddMinutes(-2 * CurrentWindowMinutes),
            now.AddMinutes(-CurrentWindowMinutes));

        var baseline = volume15m / (decimal)LongWindowMinutes;
        decimal? rvol = baseline > 0 ? decimal.Round(volume1m / baseline, 4) : null;
        decimal? acceleration = previous1m > 0
            ? decimal.Round((volume1m / (decimal)previous1m) - 1m, 4)
            : null;

        var priceChange = current.Close - previous.Close;
        var priceChangePct = previous.Close == 0
            ? 0
            : decimal.Round(priceChange / previous.Close * 100m, 4);

        var vwap = CalculateVwap(oldestFirst, now.AddMinutes(-LongWindowMinutes), now);
        var vwapDeviation = vwap == 0 ? 0 : decimal.Round((current.Close - vwap) / vwap * 100m, 4);

        var prior = oldestFirst.Take(oldestFirst.Count - 1)
            .Where(s => s.Timestamp > now.AddMinutes(-LongWindowMinutes))
            .ToList();
        var breakoutHigh = prior.Count > 0 && current.Close > prior.Max(s => s.High);
        var breakoutLow = prior.Count > 0 && current.Close < prior.Min(s => s.Low);

        var span = now - oldestFirst[0].Timestamp;

        return new MarketFeatures
        {
            Symbol = symbol,
            SourceEventId = current.EventId,
            CalculatedAt = now,
            LastPrice = current.Close,
            LastVolume = current.Volume,
            Volume1m = volume1m,
            Volume5m = volume5m,
            Volume15m = volume15m,
            BaselineVolumePerMinute = decimal.Round(baseline, 2),
            RelativeVolume = rvol,
            VolumeAcceleration = acceleration,
            PriceChange = decimal.Round(priceChange, 4),
            PriceChangePercent = priceChangePct,
            Vwap = decimal.Round(vwap, 4),
            VwapDeviationPercent = vwapDeviation,
            BreakoutHigh = breakoutHigh,
            BreakoutLow = breakoutLow,
            Volatility = decimal.Round(CalculateVolatility(oldestFirst, now.AddMinutes(-LongWindowMinutes), now), 6),
            SampleCount = oldestFirst.Count,
            HasSufficientHistory = oldestFirst.Count >= 10 || span >= TimeSpan.FromMinutes(5)
        };
    }

    private static long SumVolume(IReadOnlyList<TickSample> samples, DateTimeOffset fromExclusive, DateTimeOffset toInclusive)
    {
        long sum = 0;
        foreach (var sample in samples)
        {
            if (sample.Timestamp > fromExclusive && sample.Timestamp <= toInclusive)
            {
                sum += sample.Volume;
            }
        }

        return sum;
    }

    private static decimal CalculateVwap(IReadOnlyList<TickSample> samples, DateTimeOffset fromExclusive, DateTimeOffset toInclusive)
    {
        decimal pv = 0;
        decimal volume = 0;
        foreach (var sample in samples)
        {
            if (sample.Timestamp <= fromExclusive || sample.Timestamp > toInclusive)
            {
                continue;
            }

            pv += sample.TypicalPrice * sample.Volume;
            volume += sample.Volume;
        }

        return volume == 0 ? samples[^1].Close : pv / volume;
    }

    private static decimal CalculateVolatility(IReadOnlyList<TickSample> samples, DateTimeOffset fromExclusive, DateTimeOffset toInclusive)
    {
        var window = samples.Where(s => s.Timestamp > fromExclusive && s.Timestamp <= toInclusive).ToList();
        if (window.Count < 2)
        {
            return 0;
        }

        var returns = new List<decimal>(window.Count - 1);
        for (var i = 1; i < window.Count; i++)
        {
            if (window[i - 1].Close == 0)
            {
                continue;
            }

            returns.Add((window[i].Close - window[i - 1].Close) / window[i - 1].Close);
        }

        if (returns.Count == 0)
        {
            return 0;
        }

        var mean = returns.Average();
        var variance = returns.Sum(r => (r - mean) * (r - mean)) / returns.Count;
        return (decimal)Math.Sqrt((double)variance);
    }
}
