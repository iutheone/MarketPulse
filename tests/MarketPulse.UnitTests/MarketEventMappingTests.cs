using MarketPulse.Application.Mapping;
using MarketPulse.Domain.Entities;
using MarketPulse.Domain.Events;
using MarketPulse.Infrastructure.Kafka;

namespace MarketPulse.UnitTests;

public class MarketEventMappingTests
{
    [Fact]
    public void ToTick_CopiesBarFields_AndAssignsCanonicalSchema()
    {
        var bar = new MarketBar
        {
            Symbol = "AAPL",
            Timestamp = new DateTimeOffset(2026, 9, 19, 10, 0, 0, TimeSpan.Zero),
            Open = 190m,
            High = 191m,
            Low = 189.5m,
            Close = 190.4m,
            Volume = 10_000,
            Source = "synthetic"
        };

        var eventId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var tick = MarketEventMapper.ToTick(bar, eventId);

        Assert.Equal(eventId, tick.EventId);
        Assert.Equal(bar.Symbol, tick.Symbol);
        Assert.Equal(bar.Close, tick.Close);
        Assert.Equal(bar.Volume, tick.Volume);
        Assert.Equal(SchemaVersions.MarketTick, tick.SchemaVersion);
    }

    [Fact]
    public void ToTick_WithoutExplicitId_IsStableForTheSameBar()
    {
        var bar = new MarketBar
        {
            Symbol = "AAPL",
            Timestamp = new DateTimeOffset(2026, 9, 19, 10, 0, 0, TimeSpan.Zero),
            Open = 1, High = 1, Low = 1, Close = 1, Volume = 1,
            Source = "twelvedata"
        };

        var first = MarketEventMapper.ToTick(bar);
        var second = MarketEventMapper.ToTick(bar);

        Assert.Equal(first.EventId, second.EventId);
    }

    [Fact]
    public void ToEnvelope_UsesTickIdentity()
    {
        var tick = new MarketTick
        {
            EventId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            Symbol = "MSFT",
            Timestamp = new DateTimeOffset(2026, 9, 19, 11, 0, 0, TimeSpan.Zero),
            Open = 1,
            High = 2,
            Low = 1,
            Close = 1.5m,
            Volume = 100,
            Source = "synthetic",
            SchemaVersion = 1
        };

        var envelope = MarketEventMapper.ToEnvelope(tick);

        Assert.Equal(tick.EventId, envelope.EventId);
        Assert.Equal("market.tick.normalized", envelope.EventType);
        Assert.Equal(tick, envelope.Data);
        Assert.Equal(tick.Timestamp, envelope.OccurredAt);
    }

    [Fact]
    public void JsonSerializer_RoundTripsEnvelope()
    {
        var tick = new MarketTick
        {
            EventId = Guid.NewGuid(),
            Symbol = "NVDA",
            Timestamp = DateTimeOffset.UtcNow,
            Open = 120,
            High = 121,
            Low = 119.5m,
            Close = 120.4m,
            Volume = 9_001,
            Source = "synthetic"
        };

        var serializer = new JsonEventSerializer();
        var json = serializer.Serialize(MarketEventMapper.ToEnvelope(tick));
        var restored = serializer.Deserialize<MarketTick>(json);

        Assert.NotNull(restored);
        Assert.Equal(tick.EventId, restored!.EventId);
        Assert.Equal(tick.Symbol, restored.Data.Symbol);
        Assert.Equal(tick.Volume, restored.Data.Volume);
        Assert.Contains("schemaVersion", json, StringComparison.Ordinal);
    }
}
