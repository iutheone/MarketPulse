using MarketPulse.Domain.Features;

namespace MarketPulse.UnitTests;

public class FeatureCalculatorTests
{
    private readonly FeatureCalculator _calculator = new();
    private static readonly DateTimeOffset T0 = new(2026, 9, 19, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RollingVolumes_UseEventTimeWindows()
    {
        var samples = new[]
        {
            Sample(T0, 100),
            Sample(T0.AddMinutes(10), 500)
        };

        var features = _calculator.Calculate("AAPL", samples);

        Assert.Equal(500, features.Volume1m);
        Assert.Equal(500, features.Volume5m);
        Assert.Equal(600, features.Volume15m);
        Assert.Equal(40.00m, features.BaselineVolumePerMinute);
        Assert.Equal(12.5m, features.RelativeVolume);
    }

    [Fact]
    public void VolumeAcceleration_ComparesCurrentMinuteToPreviousMinute()
    {
        var samples = new[]
        {
            Sample(T0, 100),
            Sample(T0.AddSeconds(30), 100),
            Sample(T0.AddMinutes(1), 50),
            Sample(T0.AddMinutes(1).AddSeconds(20), 200),
            Sample(T0.AddMinutes(2), 200)
        };

        var features = _calculator.Calculate("AAPL", samples);

        Assert.Equal(400, features.Volume1m);
        Assert.Equal(150, SumExpectedPreviousMinute());
        Assert.Equal(decimal.Round(400m / 150m - 1m, 4), features.VolumeAcceleration);
    }

    [Fact]
    public void Vwap_IsVolumeWeightedTypicalPrice()
    {
        var samples = new[]
        {
            new TickSample
            {
                EventId = Guid.NewGuid(),
                Timestamp = T0,
                Open = 10, High = 10, Low = 10, Close = 10, Volume = 100
            },
            new TickSample
            {
                EventId = Guid.NewGuid(),
                Timestamp = T0.AddSeconds(10),
                Open = 12, High = 12, Low = 12, Close = 12, Volume = 300
            }
        };

        var features = _calculator.Calculate("MSFT", samples);

        Assert.Equal(11.5m, features.Vwap);
        Assert.Equal(4.3478m, features.VwapDeviationPercent);
    }

    [Fact]
    public void PriceChange_UsesPreviousClose()
    {
        var samples = new[]
        {
            Sample(T0, 100, close: 100),
            Sample(T0.AddSeconds(10), 50, close: 110)
        };

        var features = _calculator.Calculate("NVDA", samples);

        Assert.Equal(10m, features.PriceChange);
        Assert.Equal(10m, features.PriceChangePercent);
        Assert.Equal(110m, features.LastPrice);
    }

    [Fact]
    public void BreakoutHigh_WhenCloseExceedsPriorHighs()
    {
        var samples = new[]
        {
            new TickSample
            {
                EventId = Guid.NewGuid(),
                Timestamp = T0,
                Open = 100, High = 101, Low = 99, Close = 100, Volume = 10
            },
            new TickSample
            {
                EventId = Guid.NewGuid(),
                Timestamp = T0.AddSeconds(10),
                Open = 100, High = 102, Low = 99, Close = 101, Volume = 10
            },
            new TickSample
            {
                EventId = Guid.NewGuid(),
                Timestamp = T0.AddSeconds(20),
                Open = 101, High = 108, Low = 101, Close = 107, Volume = 10
            }
        };

        var features = _calculator.Calculate("TSLA", samples);

        Assert.True(features.BreakoutHigh);
        Assert.False(features.BreakoutLow);
    }

    private static long SumExpectedPreviousMinute()
    {
        // (T0+2m - 2m, T0+2m - 1m] = (T0, T0+1m] => 100 (30s) + 50 (1m) = 150
        return 150;
    }

    private static TickSample Sample(DateTimeOffset timestamp, long volume, decimal close = 100)
    {
        return new TickSample
        {
            EventId = Guid.NewGuid(),
            Timestamp = timestamp,
            Open = close,
            High = close,
            Low = close,
            Close = close,
            Volume = volume
        };
    }
}
