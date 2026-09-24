using MarketPulse.Application.Mapping;
using MarketPulse.Application.Messaging;
using MarketPulse.Domain.Anomaly;
using MarketPulse.Domain.Enums;
using MarketPulse.Domain.Events;
using MarketPulse.Infrastructure.Persistence;

namespace MarketPulse.UnitTests;

public class AnomalyMappingTests
{
    [Fact]
    public void Envelope_UsesAnomalyEventTypeAndSymbolPayload()
    {
        var anomaly = Sample();
        var envelope = AnomalyEventMapper.ToEnvelope(anomaly);

        Assert.Equal(anomaly.AnomalyId, envelope.EventId);
        Assert.Equal(EventTypes.AnomalyDetected, envelope.EventType);
        Assert.Equal(SchemaVersions.Anomaly, envelope.SchemaVersion);
        Assert.Equal(anomaly.Symbol, envelope.Data.Symbol);
    }

    [Fact]
    public void StoreMapping_RoundTripsReasonsAndSeverity()
    {
        var anomaly = Sample();
        var row = PostgresAnomalyStore.ToRecord(anomaly);
        var restored = PostgresAnomalyStore.FromRecord(row);

        Assert.Equal(anomaly.AnomalyId, restored.AnomalyId);
        Assert.Equal(anomaly.Severity, restored.Severity);
        Assert.Equal(anomaly.Reasons, restored.Reasons);
        Assert.Equal(anomaly.Score, restored.Score);
    }

    [Fact]
    public void ApiDto_DoesNotExposeDomainTypeNamesAsPayloadShape()
    {
        var anomaly = Sample();
        var dto = ApiDtoMapper.ToDto(anomaly);
        var live = ApiDtoMapper.ToRealtime(anomaly);

        Assert.Equal("High", dto.Severity);
        Assert.Equal(dto.Id, live.Id);
        Assert.Equal(dto.DetectedAt, live.Timestamp);
        Assert.Equal(dto.Reasons, live.Reasons);
    }

    private static AnomalyResult Sample() => new()
    {
        AnomalyId = Guid.NewGuid(),
        SourceEventId = Guid.NewGuid(),
        Symbol = "MSFT",
        DetectedAt = new DateTimeOffset(2026, 9, 20, 15, 0, 0, TimeSpan.Zero),
        Score = 62.5m,
        Severity = AnomalySeverity.High,
        Reasons = ["RVOL 3.0x", "Price above VWAP"],
        RuleVersion = "v1",
        RelativeVolume = 3,
        VolumeAcceleration = 1,
        PriceChangePercent = 1.5m,
        VwapDeviationPercent = 0.8m,
        Breakout = false,
        LastPrice = 420,
        Volume1m = 8000
    };
}
