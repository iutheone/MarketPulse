using MarketPulse.Domain.Events;
using MarketPulse.Infrastructure.Redis;

namespace MarketPulse.UnitTests;

public class RedisWindowEncodingTests
{
    [Fact]
    public void MemberRoundTrip_PreservesOhlcvAndEventId()
    {
        var tick = new MarketTick
        {
            EventId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            Symbol = "AAPL",
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(1_726_747_200_000),
            Open = 190.1m,
            High = 191.2m,
            Low = 189.9m,
            Close = 190.8m,
            Volume = 48_000,
            Source = "synthetic"
        };

        var member = RedisMarketFeatureStore.FormatMember(tick);
        Assert.True(RedisMarketFeatureStore.TryParseMember(member, out var sample));
        Assert.Equal(tick.EventId, sample.EventId);
        Assert.Equal(tick.Close, sample.Close);
        Assert.Equal(tick.Volume, sample.Volume);
        Assert.Equal(tick.Timestamp, sample.Timestamp);
    }
}
