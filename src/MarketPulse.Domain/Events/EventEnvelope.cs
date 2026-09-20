namespace MarketPulse.Domain.Events;

/// <summary>
/// Versioned envelope around a domain event. JSON is the Phase 1 payload format;
/// the envelope exists so Avro/Protobuf can replace the body later without changing Kafka keys.
/// </summary>
public sealed record EventEnvelope<T>
{
    public required Guid EventId { get; init; }
    public required string EventType { get; init; }
    public required int SchemaVersion { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required T Data { get; init; }
}
