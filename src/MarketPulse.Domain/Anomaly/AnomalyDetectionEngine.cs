using MarketPulse.Domain.Enums;
using MarketPulse.Domain.Features;
using MarketPulse.Domain.Interfaces;

namespace MarketPulse.Domain.Anomaly;

public sealed class AnomalyDetectionEngine : IAnomalyDetectionEngine
{
    private readonly DetectionParameters _parameters;

    public AnomalyDetectionEngine(DetectionParameters parameters)
    {
        _parameters = parameters;
    }

    public AnomalyResult? Evaluate(MarketFeatures features)
    {
        if (!features.HasSufficientHistory)
        {
            return null;
        }

        var rvolComponent = Normalize(features.RelativeVolume, _parameters.RvolFullScale);
        var accelComponent = Normalize(features.VolumeAcceleration, _parameters.AccelerationFullScale);
        var priceComponent = Normalize(Math.Abs(features.PriceChangePercent), _parameters.AbsPriceChangeFullScale);
        var vwapComponent = Normalize(Math.Abs(features.VwapDeviationPercent), _parameters.VwapDeviationFullScale);
        var breakout = features.BreakoutHigh || features.BreakoutLow;
        var breakoutComponent = breakout ? 1m : 0m;

        var score = decimal.Round(
            100m * (
                _parameters.WeightRvol * rvolComponent +
                _parameters.WeightAcceleration * accelComponent +
                _parameters.WeightAbsPriceChange * priceComponent +
                _parameters.WeightVwapDeviation * vwapComponent +
                _parameters.WeightBreakout * breakoutComponent),
            1);

        if (score < _parameters.MinScoreToRecord)
        {
            return null;
        }

        return new AnomalyResult
        {
            AnomalyId = Guid.NewGuid(),
            SourceEventId = features.SourceEventId,
            Symbol = features.Symbol,
            DetectedAt = features.CalculatedAt,
            Score = score,
            Severity = Classify(score),
            Reasons = BuildReasons(features, breakout),
            RuleVersion = _parameters.RuleVersion,
            RelativeVolume = features.RelativeVolume,
            VolumeAcceleration = features.VolumeAcceleration,
            PriceChangePercent = features.PriceChangePercent,
            VwapDeviationPercent = features.VwapDeviationPercent,
            Breakout = breakout,
            LastPrice = features.LastPrice,
            Volume1m = features.Volume1m
        };
    }

    private static decimal Normalize(decimal? value, decimal fullScale)
    {
        if (value is null || fullScale <= 0)
        {
            return 0;
        }

        var unit = Math.Abs(value.Value) / fullScale;
        if (unit < 0) return 0;
        if (unit > 1) return 1;
        return unit;
    }

    private static AnomalySeverity Classify(decimal score)
    {
        if (score >= 80m) return AnomalySeverity.Extreme;
        if (score >= 60m) return AnomalySeverity.High;
        if (score >= 40m) return AnomalySeverity.Elevated;
        return AnomalySeverity.Informational;
    }

    private static IReadOnlyList<string> BuildReasons(MarketFeatures features, bool breakout)
    {
        var reasons = new List<string>();
        if (features.RelativeVolume is > 1.5m)
        {
            reasons.Add($"RVOL {features.RelativeVolume.Value:0.0}x");
        }

        if (features.VolumeAcceleration is > 0.5m)
        {
            reasons.Add("Volume acceleration elevated");
        }

        if (Math.Abs(features.PriceChangePercent) >= 1m)
        {
            reasons.Add($"Price moved {features.PriceChangePercent:0.0}%");
        }

        if (features.VwapDeviationPercent > 0.5m)
        {
            reasons.Add("Price above VWAP");
        }
        else if (features.VwapDeviationPercent < -0.5m)
        {
            reasons.Add("Price below VWAP");
        }

        if (features.BreakoutHigh)
        {
            reasons.Add("Close broke local high");
        }
        else if (features.BreakoutLow)
        {
            reasons.Add("Close broke local low");
        }

        if (reasons.Count == 0 && breakout)
        {
            reasons.Add("Breakout context");
        }

        if (reasons.Count == 0)
        {
            reasons.Add("Combined feature score crossed threshold");
        }

        return reasons;
    }
}
