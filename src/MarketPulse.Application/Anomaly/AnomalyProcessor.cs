using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Options;
using MarketPulse.Domain.Anomaly;
using MarketPulse.Domain.Events;
using MarketPulse.Domain.Features;
using MarketPulse.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MarketPulse.Application.Anomaly;

public sealed class AnomalyProcessor : IAnomalyProcessor
{
    private readonly IAnomalyDetectionEngine _engine;
    private readonly IAnomalyStore _store;
    private readonly IAnomalyEventPublisher _publisher;
    private readonly IAnomalyFeed _feed;
    private readonly ILogger<AnomalyProcessor> _logger;

    public AnomalyProcessor(
        IAnomalyDetectionEngine engine,
        IAnomalyStore store,
        IAnomalyEventPublisher publisher,
        IAnomalyFeed feed,
        ILogger<AnomalyProcessor> logger)
    {
        _engine = engine;
        _store = store;
        _publisher = publisher;
        _feed = feed;
        _logger = logger;
    }

    public async Task ProcessAsync(MarketTick tick, MarketFeatures features, CancellationToken cancellationToken)
    {
        var result = _engine.Evaluate(features);
        var recorded = await _store.TryRecordAsync(tick, result, cancellationToken);
        if (!recorded)
        {
            _logger.LogInformation("Skipped duplicate event {EventId} for {Symbol}", tick.EventId, tick.Symbol);
            return;
        }

        if (result is null)
        {
            return;
        }

        await _feed.PushAsync(result, cancellationToken);
        await _publisher.PublishAsync(result, cancellationToken);
        _logger.LogInformation(
            "Recorded anomaly {AnomalyId} for {Symbol} score={Score} severity={Severity} event={EventId}",
            result.AnomalyId,
            result.Symbol,
            result.Score,
            result.Severity,
            result.SourceEventId);
    }
}

public static class AnomalyDetectionOptionsExtensions
{
    public static DetectionParameters ToParameters(this AnomalyDetectionOptions options) => new()
    {
        RuleVersion = options.RuleVersion,
        MinScoreToRecord = options.MinScoreToRecord,
        RvolFullScale = options.RvolFullScale,
        AccelerationFullScale = options.AccelerationFullScale,
        AbsPriceChangeFullScale = options.AbsPriceChangeFullScale,
        VwapDeviationFullScale = options.VwapDeviationFullScale,
        WeightRvol = options.WeightRvol,
        WeightAcceleration = options.WeightAcceleration,
        WeightAbsPriceChange = options.WeightAbsPriceChange,
        WeightVwapDeviation = options.WeightVwapDeviation,
        WeightBreakout = options.WeightBreakout
    };
}
