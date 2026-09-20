using MarketPulse.Application.Options;
using MarketPulse.Infrastructure.MarketData.Synthetic;
using Microsoft.Extensions.Options;

namespace MarketPulse.UnitTests;

public class SyntheticMarketDataProviderTests
{
    [Fact]
    public async Task GetLatestAsync_IsDeterministic_ForTheSameSeed()
    {
        var options = Options.Create(new SyntheticMarketDataOptions
        {
            Seed = 42,
            Symbols = ["AAPL", "MSFT"],
            VolumeSpikeProbability = 0,
            PriceSpikeProbability = 0
        });

        var first = new SyntheticMarketDataProvider(options);
        var second = new SyntheticMarketDataProvider(options);

        var a = await first.GetLatestAsync(["AAPL", "MSFT"], CancellationToken.None);
        var b = await second.GetLatestAsync(["AAPL", "MSFT"], CancellationToken.None);

        Assert.Equal(2, a.Count);
        Assert.Equal(a[0].Close, b[0].Close);
        Assert.Equal(a[1].Volume, b[1].Volume);
        Assert.Equal("AAPL", a[0].Symbol);
        Assert.Equal("synthetic", a[0].Source);
    }

    [Fact]
    public async Task GetLatestAsync_ProducesConsistentOhlc()
    {
        var provider = new SyntheticMarketDataProvider(Options.Create(new SyntheticMarketDataOptions
        {
            Seed = 7,
            Symbols = ["NVDA"]
        }));

        var bars = await provider.GetLatestAsync(["NVDA"], CancellationToken.None);
        var bar = Assert.Single(bars);

        Assert.True(bar.High >= bar.Open);
        Assert.True(bar.High >= bar.Close);
        Assert.True(bar.Low <= bar.Open);
        Assert.True(bar.Low <= bar.Close);
        Assert.True(bar.Volume > 0);
    }

    [Fact]
    public async Task GetLatestAsync_CanEmitVolumeSpikes()
    {
        var provider = new SyntheticMarketDataProvider(Options.Create(new SyntheticMarketDataOptions
        {
            Seed = 99,
            Symbols = ["TSLA"],
            VolumeSpikeProbability = 1,
            PriceSpikeProbability = 0
        }));

        var quiet = new SyntheticMarketDataProvider(Options.Create(new SyntheticMarketDataOptions
        {
            Seed = 99,
            Symbols = ["TSLA"],
            VolumeSpikeProbability = 0,
            PriceSpikeProbability = 0
        }));

        var spiked = await provider.GetLatestAsync(["TSLA"], CancellationToken.None);
        var baseline = await quiet.GetLatestAsync(["TSLA"], CancellationToken.None);

        Assert.True(spiked[0].Volume > baseline[0].Volume * 4);
    }

    [Fact]
    public async Task StreamAsync_YieldsThenStopsOnCancellation()
    {
        var provider = new SyntheticMarketDataProvider(Options.Create(new SyntheticMarketDataOptions
        {
            Seed = 1,
            Symbols = ["AMZN"],
            IntervalMilliseconds = 50
        }));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        var count = 0;

        await foreach (var tick in provider.StreamAsync(["AMZN"], cts.Token))
        {
            Assert.Equal("AMZN", tick.Symbol);
            Assert.Equal(1, tick.SchemaVersion);
            count++;
            if (count >= 2)
            {
                break;
            }
        }

        Assert.True(count >= 2);
    }
}
