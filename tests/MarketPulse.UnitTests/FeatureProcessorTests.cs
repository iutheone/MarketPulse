using MarketPulse.Application.Features;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Options;
using MarketPulse.Domain.Events;
using MarketPulse.Domain.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MarketPulse.UnitTests;

public class FeatureProcessorTests
{
    [Fact]
    public async Task ProcessAsync_StoresFeaturesFromAppendedTicks()
    {
        var store = new InMemoryMarketFeatureStore();
        var processor = new FeatureProcessor(
            store,
            new FeatureCalculator(),
            Options.Create(new FeatureProcessingOptions { RetentionMinutes = 60 }),
            NullLogger<FeatureProcessor>.Instance);

        var first = Tick("NVDA", 100, 1_000, new DateTimeOffset(2026, 9, 19, 15, 0, 0, TimeSpan.Zero));
        var second = Tick("NVDA", 102, 1_200, new DateTimeOffset(2026, 9, 19, 15, 0, 30, TimeSpan.Zero));

        await processor.ProcessAsync(first, CancellationToken.None);
        await processor.ProcessAsync(second, CancellationToken.None);

        var features = await store.GetFeaturesAsync("NVDA", CancellationToken.None);
        Assert.NotNull(features);
        Assert.Equal(102m, features!.LastPrice);
        Assert.Equal(2_200, features.Volume1m);
        Assert.Equal(2, features.SampleCount);
        Assert.Equal(second.EventId, features.SourceEventId);
    }

    private static MarketTick Tick(string symbol, decimal close, long volume, DateTimeOffset timestamp)
    {
        return new MarketTick
        {
            EventId = Guid.NewGuid(),
            Symbol = symbol,
            Timestamp = timestamp,
            Open = close,
            High = close,
            Low = close,
            Close = close,
            Volume = volume,
            Source = "test"
        };
    }

    private sealed class InMemoryMarketFeatureStore : IMarketFeatureStore
    {
        private readonly Dictionary<string, List<TickSample>> _windows = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, MarketFeatures> _features = new(StringComparer.OrdinalIgnoreCase);

        public Task AppendAsync(MarketTick tick, TimeSpan retention, CancellationToken cancellationToken)
        {
            if (!_windows.TryGetValue(tick.Symbol, out var list))
            {
                list = [];
                _windows[tick.Symbol] = list;
            }

            if (list.All(s => s.EventId != tick.EventId))
            {
                list.Add(new TickSample
                {
                    EventId = tick.EventId,
                    Timestamp = tick.Timestamp,
                    Open = tick.Open,
                    High = tick.High,
                    Low = tick.Low,
                    Close = tick.Close,
                    Volume = tick.Volume
                });
            }

            var cutoff = tick.Timestamp - retention;
            _windows[tick.Symbol] = list.Where(s => s.Timestamp >= cutoff).OrderBy(s => s.Timestamp).ToList();
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<TickSample>> GetWindowAsync(
            string symbol,
            DateTimeOffset fromInclusive,
            CancellationToken cancellationToken)
        {
            if (!_windows.TryGetValue(symbol, out var list))
            {
                return Task.FromResult<IReadOnlyList<TickSample>>([]);
            }

            return Task.FromResult<IReadOnlyList<TickSample>>(
                list.Where(s => s.Timestamp >= fromInclusive).ToList());
        }

        public Task SaveFeaturesAsync(MarketFeatures features, CancellationToken cancellationToken)
        {
            _features[features.Symbol] = features;
            return Task.CompletedTask;
        }

        public Task<MarketFeatures?> GetFeaturesAsync(string symbol, CancellationToken cancellationToken)
        {
            _features.TryGetValue(symbol, out var features);
            return Task.FromResult(features);
        }
    }
}
