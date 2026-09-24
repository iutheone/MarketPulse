using MarketPulse.Application.Anomaly;
using MarketPulse.Application.Interfaces;
using MarketPulse.Domain.Anomaly;
using MarketPulse.Domain.Events;
using MarketPulse.Domain.Features;
using MarketPulse.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace MarketPulse.UnitTests;

public class AnomalyProcessorTests
{
    [Fact]
    public async Task ProcessAsync_DoesNotPublishWhenStoreReportsDuplicate()
    {
        var store = new RecordingStore { Duplicate = true };
        var publisher = new RecordingPublisher();
        var feed = new RecordingFeed();
        var processor = new AnomalyProcessor(
            new AlwaysAnomalyEngine(),
            store,
            publisher,
            feed,
            NullLogger<AnomalyProcessor>.Instance);

        await processor.ProcessAsync(Tick(), Features(), CancellationToken.None);

        Assert.Equal(1, store.Calls);
        Assert.Equal(0, publisher.Calls);
        Assert.Equal(0, feed.Calls);
    }

    [Fact]
    public async Task ProcessAsync_PersistsQuietTicksWithoutPublishing()
    {
        var store = new RecordingStore();
        var publisher = new RecordingPublisher();
        var feed = new RecordingFeed();
        var processor = new AnomalyProcessor(
            new QuietEngine(),
            store,
            publisher,
            feed,
            NullLogger<AnomalyProcessor>.Instance);

        await processor.ProcessAsync(Tick(), Features(), CancellationToken.None);

        Assert.Equal(1, store.Calls);
        Assert.Null(store.LastAnomaly);
        Assert.Equal(0, publisher.Calls);
        Assert.Equal(0, feed.Calls);
    }

    [Fact]
    public async Task ProcessAsync_PublishesAfterSuccessfulRecord()
    {
        var store = new RecordingStore();
        var publisher = new RecordingPublisher();
        var feed = new RecordingFeed();
        var processor = new AnomalyProcessor(
            new AlwaysAnomalyEngine(),
            store,
            publisher,
            feed,
            NullLogger<AnomalyProcessor>.Instance);

        await processor.ProcessAsync(Tick(), Features(), CancellationToken.None);

        Assert.Equal(1, store.Calls);
        Assert.Equal(1, feed.Calls);
        Assert.Equal(1, publisher.Calls);
        Assert.Equal(store.LastAnomaly!.AnomalyId, publisher.Last!.AnomalyId);
    }

    private static MarketTick Tick() => new()
    {
        EventId = Guid.NewGuid(),
        Symbol = "AAPL",
        Timestamp = DateTimeOffset.UtcNow,
        Open = 1, High = 1, Low = 1, Close = 1,
        Volume = 10,
        Source = "test"
    };

    private static MarketFeatures Features() => new()
    {
        Symbol = "AAPL",
        SourceEventId = Guid.NewGuid(),
        CalculatedAt = DateTimeOffset.UtcNow,
        LastPrice = 1,
        LastVolume = 10,
        Volume1m = 10,
        Volume5m = 10,
        Volume15m = 10,
        BaselineVolumePerMinute = 10,
        RelativeVolume = 5,
        VolumeAcceleration = 2,
        PriceChange = 1,
        PriceChangePercent = 2,
        Vwap = 1,
        VwapDeviationPercent = 1,
        BreakoutHigh = true,
        BreakoutLow = false,
        Volatility = 0,
        SampleCount = 20,
        HasSufficientHistory = true
    };

    private sealed class AlwaysAnomalyEngine : IAnomalyDetectionEngine
    {
        public AnomalyResult? Evaluate(MarketFeatures features) => new()
        {
            AnomalyId = Guid.NewGuid(),
            SourceEventId = features.SourceEventId,
            Symbol = features.Symbol,
            DetectedAt = features.CalculatedAt,
            Score = 81,
            Severity = MarketPulse.Domain.Enums.AnomalySeverity.Extreme,
            Reasons = ["test"],
            RuleVersion = "v1",
            RelativeVolume = features.RelativeVolume,
            VolumeAcceleration = features.VolumeAcceleration,
            PriceChangePercent = features.PriceChangePercent,
            VwapDeviationPercent = features.VwapDeviationPercent,
            Breakout = true,
            LastPrice = features.LastPrice,
            Volume1m = features.Volume1m
        };
    }

    private sealed class QuietEngine : IAnomalyDetectionEngine
    {
        public AnomalyResult? Evaluate(MarketFeatures features) => null;
    }

    private sealed class RecordingStore : IAnomalyStore
    {
        public bool Duplicate { get; set; }
        public int Calls { get; private set; }
        public AnomalyResult? LastAnomaly { get; private set; }

        public Task<bool> TryRecordAsync(MarketTick tick, AnomalyResult? anomaly, CancellationToken cancellationToken)
        {
            Calls++;
            LastAnomaly = anomaly;
            return Task.FromResult(!Duplicate);
        }

        public Task<AnomalyResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<AnomalyResult?>(null);

        public Task<AnomalyResult?> GetLatestBySymbolAsync(string symbol, CancellationToken cancellationToken) =>
            Task.FromResult<AnomalyResult?>(null);

        public Task<(IReadOnlyList<AnomalyResult> Items, int Total)> SearchAsync(
            MarketPulse.Application.Queries.AnomalyQuery query, CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<AnomalyResult>, int)>(([], 0));
    }

    private sealed class RecordingPublisher : IAnomalyEventPublisher
    {
        public int Calls { get; private set; }
        public AnomalyResult? Last { get; private set; }

        public Task PublishAsync(AnomalyResult anomaly, CancellationToken cancellationToken)
        {
            Calls++;
            Last = anomaly;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingFeed : IAnomalyFeed
    {
        public int Calls { get; private set; }

        public Task PushAsync(AnomalyResult anomaly, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }
}
