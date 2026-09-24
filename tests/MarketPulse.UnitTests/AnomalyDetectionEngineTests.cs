using MarketPulse.Domain.Anomaly;
using MarketPulse.Domain.Enums;
using MarketPulse.Domain.Features;

namespace MarketPulse.UnitTests;

public class AnomalyDetectionEngineTests
{
    private readonly AnomalyDetectionEngine _engine = new(new DetectionParameters());

    [Fact]
    public void Evaluate_ReturnsNull_WhenHistoryIsThin()
    {
        var result = _engine.Evaluate(Features(hasHistory: false, rvol: 9, accel: 3, pricePct: 8, vwapPct: 4, breakout: true));
        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_ReturnsNull_WhenScoreIsBelowThreshold()
    {
        var result = _engine.Evaluate(Features(rvol: 1.1m, accel: 0.1m, pricePct: 0.2m, vwapPct: 0.1m, breakout: false));
        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_ClassifiesElevatedHighAndExtreme()
    {
        var elevated = _engine.Evaluate(Features(rvol: 5, accel: 2, pricePct: 0, vwapPct: 0, breakout: false));
        Assert.NotNull(elevated);
        Assert.Equal(55.0m, elevated!.Score);
        Assert.Equal(AnomalySeverity.Elevated, elevated.Severity);

        var high = _engine.Evaluate(Features(rvol: 5, accel: 2, pricePct: 5, vwapPct: 0, breakout: false));
        Assert.NotNull(high);
        Assert.Equal(75.0m, high!.Score);
        Assert.Equal(AnomalySeverity.High, high.Severity);

        var extreme = _engine.Evaluate(Features(rvol: 5, accel: 2, pricePct: 5, vwapPct: 3, breakout: true));
        Assert.NotNull(extreme);
        Assert.Equal(100.0m, extreme!.Score);
        Assert.Equal(AnomalySeverity.Extreme, extreme.Severity);
    }

    [Fact]
    public void Evaluate_BuildsReadableReasons()
    {
        var result = _engine.Evaluate(Features(rvol: 5.2m, accel: 0.8m, pricePct: 2.1m, vwapPct: 1.2m, breakout: true));
        Assert.NotNull(result);
        Assert.Contains(result!.Reasons, r => r.StartsWith("RVOL 5.2x"));
        Assert.Contains("Volume acceleration elevated", result.Reasons);
        Assert.Contains(result.Reasons, r => r.Contains("Price moved 2.1%"));
        Assert.Contains("Price above VWAP", result.Reasons);
        Assert.Contains("Close broke local high", result.Reasons);
    }

    [Fact]
    public void Evaluate_ClampsNormalizedInputsAtOne()
    {
        var result = _engine.Evaluate(Features(rvol: 50, accel: 20, pricePct: 40, vwapPct: 30, breakout: true));
        Assert.NotNull(result);
        Assert.Equal(100.0m, result!.Score);
    }

    private static MarketFeatures Features(
        decimal rvol = 1,
        decimal accel = 0,
        decimal pricePct = 0,
        decimal vwapPct = 0,
        bool breakout = false,
        bool hasHistory = true)
    {
        return new MarketFeatures
        {
            Symbol = "NVDA",
            SourceEventId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            CalculatedAt = new DateTimeOffset(2026, 9, 20, 14, 0, 0, TimeSpan.Zero),
            LastPrice = 100,
            LastVolume = 1000,
            Volume1m = 1000,
            Volume5m = 2000,
            Volume15m = 3000,
            BaselineVolumePerMinute = 200,
            RelativeVolume = rvol,
            VolumeAcceleration = accel,
            PriceChange = pricePct,
            PriceChangePercent = pricePct,
            Vwap = 99,
            VwapDeviationPercent = vwapPct,
            BreakoutHigh = breakout,
            BreakoutLow = false,
            Volatility = 0.01m,
            SampleCount = 20,
            HasSufficientHistory = hasHistory
        };
    }
}
