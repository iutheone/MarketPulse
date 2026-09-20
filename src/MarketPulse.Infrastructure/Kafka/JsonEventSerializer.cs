using System.Text.Json;
using MarketPulse.Application.Interfaces;
using MarketPulse.Domain.Events;

namespace MarketPulse.Infrastructure.Kafka;

public sealed class JsonEventSerializer : IEventSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public string Serialize<T>(EventEnvelope<T> envelope) =>
        JsonSerializer.Serialize(envelope, Options);

    public EventEnvelope<T>? Deserialize<T>(string payload) =>
        JsonSerializer.Deserialize<EventEnvelope<T>>(payload, Options);
}
