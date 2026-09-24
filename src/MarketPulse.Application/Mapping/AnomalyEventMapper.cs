using MarketPulse.Application.Messaging;
using MarketPulse.Domain.Anomaly;
using MarketPulse.Domain.Events;

namespace MarketPulse.Application.Mapping;

public static class AnomalyEventMapper
{
    public static EventEnvelope<AnomalyResult> ToEnvelope(AnomalyResult anomaly)
    {
        return new EventEnvelope<AnomalyResult>
        {
            EventId = anomaly.AnomalyId,
            EventType = EventTypes.AnomalyDetected,
            SchemaVersion = SchemaVersions.Anomaly,
            OccurredAt = anomaly.DetectedAt,
            Data = anomaly
        };
    }
}
