using MarketPulse.Application.Observability;

namespace MarketPulse.UnitTests;

public class TelemetryTests
{
    [Fact]
    public void MeterAndActivitySource_UseStableNamesForPrometheus()
    {
        Assert.Equal("MarketPulse", MarketPulseTelemetry.MeterName);
        Assert.Equal("MarketPulse", MarketPulseTelemetry.ActivitySourceName);
        Assert.Same(MarketPulseTelemetry.Meter, MarketPulseTelemetry.Meter);
        using var activity = MarketPulseTelemetry.Activity.StartActivity("MarketPulse.Test");
        Assert.NotNull(MarketPulseTelemetry.TicksProcessed);
        Assert.NotNull(MarketPulseTelemetry.AnomaliesRecorded);
    }
}
