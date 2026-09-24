using MarketPulse.Application.Contracts;
using MarketPulse.Domain.Anomaly;
using MarketPulse.Domain.Events;
using MarketPulse.Domain.Features;

namespace MarketPulse.Application.Mapping;

public static class ApiDtoMapper
{
    public static AnomalyDto ToDto(AnomalyResult anomaly) => new()
    {
        Id = anomaly.AnomalyId,
        SourceEventId = anomaly.SourceEventId,
        Symbol = anomaly.Symbol,
        DetectedAt = anomaly.DetectedAt,
        Score = anomaly.Score,
        Severity = anomaly.Severity.ToString(),
        Reasons = anomaly.Reasons,
        RuleVersion = anomaly.RuleVersion,
        RelativeVolume = anomaly.RelativeVolume,
        VolumeAcceleration = anomaly.VolumeAcceleration,
        PriceChangePercent = anomaly.PriceChangePercent,
        VwapDeviationPercent = anomaly.VwapDeviationPercent,
        Breakout = anomaly.Breakout,
        LastPrice = anomaly.LastPrice,
        Volume1m = anomaly.Volume1m
    };

    public static AnomalyRealtimeDto ToRealtime(AnomalyResult anomaly) => new()
    {
        Id = anomaly.AnomalyId,
        Symbol = anomaly.Symbol,
        Score = anomaly.Score,
        Severity = anomaly.Severity.ToString(),
        Timestamp = anomaly.DetectedAt,
        Reasons = anomaly.Reasons
    };

    public static FeatureDto ToDto(MarketFeatures features) => new()
    {
        Symbol = features.Symbol,
        SourceEventId = features.SourceEventId,
        CalculatedAt = features.CalculatedAt,
        LastPrice = features.LastPrice,
        LastVolume = features.LastVolume,
        Volume1m = features.Volume1m,
        Volume5m = features.Volume5m,
        Volume15m = features.Volume15m,
        BaselineVolumePerMinute = features.BaselineVolumePerMinute,
        RelativeVolume = features.RelativeVolume,
        VolumeAcceleration = features.VolumeAcceleration,
        PriceChange = features.PriceChange,
        PriceChangePercent = features.PriceChangePercent,
        Vwap = features.Vwap,
        VwapDeviationPercent = features.VwapDeviationPercent,
        BreakoutHigh = features.BreakoutHigh,
        BreakoutLow = features.BreakoutLow,
        Volatility = features.Volatility,
        SampleCount = features.SampleCount,
        HasSufficientHistory = features.HasSufficientHistory
    };

    public static BarDto ToDto(MarketTick tick) => new()
    {
        EventId = tick.EventId,
        Symbol = tick.Symbol,
        Timestamp = tick.Timestamp,
        Open = tick.Open,
        High = tick.High,
        Low = tick.Low,
        Close = tick.Close,
        Volume = tick.Volume,
        Source = tick.Source
    };
}
