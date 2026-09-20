using MarketPulse.Domain.Entities;
using MarketPulse.Domain.Events;
using MarketPulse.Application.Messaging;
using System.Security.Cryptography;
using System.Text;

namespace MarketPulse.Application.Mapping;

public static class MarketEventMapper
{
    public static MarketTick ToTick(MarketBar bar, Guid? eventId = null)
    {
        return new MarketTick
        {
            EventId = eventId ?? CreateDeterministicEventId(bar),
            Symbol = bar.Symbol,
            Timestamp = bar.Timestamp,
            Open = bar.Open,
            High = bar.High,
            Low = bar.Low,
            Close = bar.Close,
            Volume = bar.Volume,
            Source = bar.Source,
            SchemaVersion = SchemaVersions.MarketTick
        };
    }

    public static EventEnvelope<MarketTick> ToEnvelope(MarketTick tick)
    {
        return new EventEnvelope<MarketTick>
        {
            EventId = tick.EventId,
            EventType = EventTypes.MarketTickNormalized,
            SchemaVersion = tick.SchemaVersion,
            OccurredAt = tick.Timestamp,
            Data = tick
        };
    }

    public static Guid CreateDeterministicEventId(MarketBar bar)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{bar.Source}|{bar.Symbol}|{bar.Timestamp:o}"));
        return new Guid(bytes.AsSpan(0, 16));
    }
}
