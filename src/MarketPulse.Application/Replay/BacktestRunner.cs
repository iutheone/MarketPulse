using MarketPulse.Application.Contracts;
using MarketPulse.Application.Mapping;
using MarketPulse.Domain.Anomaly;
using MarketPulse.Domain.Entities;
using MarketPulse.Domain.Events;
using MarketPulse.Domain.Features;
using MarketPulse.Domain.Interfaces;

namespace MarketPulse.Application.Replay;

public sealed class BacktestSummary
{
    public required string Symbol { get; init; }
    public required int Bars { get; init; }
    public required int Anomalies { get; init; }
    public required decimal RecordRate { get; init; }
    public decimal? MeanScore { get; init; }
}

/// <summary>
/// Walks bars in event time through the same FeatureCalculator and AnomalyDetectionEngine as live.
/// Does not publish Kafka and is not a trading simulation.
/// </summary>
public sealed class BacktestRunner
{
    private readonly FeatureCalculator _calculator;
    private readonly IAnomalyDetectionEngine _engine;
    private readonly TimeSpan _retention;

    public BacktestRunner(FeatureCalculator calculator, IAnomalyDetectionEngine engine, TimeSpan retention)
    {
        _calculator = calculator;
        _engine = engine;
        _retention = retention;
    }

    public IReadOnlyList<BacktestSummary> Run(IReadOnlyList<MarketBar> bars)
    {
        return bars
            .GroupBy(b => b.Symbol, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .Select(RunSymbol)
            .ToList();
    }

    private BacktestSummary RunSymbol(IGrouping<string, MarketBar> group)
    {
        var window = new List<TickSample>();
        var scores = new List<decimal>();
        var anomalies = 0;
        var ordered = group.OrderBy(b => b.Timestamp).ToList();

        foreach (var bar in ordered)
        {
            var tick = MarketEventMapper.ToTick(bar);
            window.Add(ToSample(tick));
            var cutoff = tick.Timestamp - _retention;
            window = window.Where(s => s.Timestamp >= cutoff).OrderBy(s => s.Timestamp).ToList();
            var features = _calculator.Calculate(tick.Symbol, window);
            var result = _engine.Evaluate(features);
            if (result is null)
            {
                continue;
            }

            anomalies++;
            scores.Add(result.Score);
        }

        return new BacktestSummary
        {
            Symbol = group.Key,
            Bars = ordered.Count,
            Anomalies = anomalies,
            RecordRate = ordered.Count == 0 ? 0 : decimal.Round(anomalies / (decimal)ordered.Count, 4),
            MeanScore = scores.Count == 0 ? null : decimal.Round(scores.Average(), 1)
        };
    }

    private static TickSample ToSample(MarketTick tick) => new()
    {
        EventId = tick.EventId,
        Timestamp = tick.Timestamp,
        Open = tick.Open,
        High = tick.High,
        Low = tick.Low,
        Close = tick.Close,
        Volume = tick.Volume
    };
}

public static class BacktestNotes
{
    public const string Disclaimer =
        "RecordRate is anomalies / bars for this rule set. It is not hit-rate, forecast accuracy, or a trading result.";
}
