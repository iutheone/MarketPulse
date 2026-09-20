using MarketPulse.Domain.Events;

namespace MarketPulse.Application.Interfaces;

/// <summary>
/// Payload codec for event envelopes. JSON in Phase 1; a future Avro serializer can replace this registration.
/// </summary>
public interface IEventSerializer
{
    string Serialize<T>(EventEnvelope<T> envelope);

    EventEnvelope<T>? Deserialize<T>(string payload);
}
