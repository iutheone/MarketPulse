using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Options;
using MarketPulse.Domain.Events;
using MarketPulse.Domain.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketPulse.Application.Features;

public sealed class FeatureProcessor : IFeatureProcessor
{
    private readonly IMarketFeatureStore _store;
    private readonly FeatureCalculator _calculator;
    private readonly FeatureProcessingOptions _options;
    private readonly IAnomalyProcessor _anomalyProcessor;
    private readonly ILogger<FeatureProcessor> _logger;

    public FeatureProcessor(
        IMarketFeatureStore store,
        FeatureCalculator calculator,
        IAnomalyProcessor anomalyProcessor,
        IOptions<FeatureProcessingOptions> options,
        ILogger<FeatureProcessor> logger)
    {
        _store = store;
        _calculator = calculator;
        _anomalyProcessor = anomalyProcessor;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProcessAsync(MarketTick tick, CancellationToken cancellationToken)
    {
        var retention = TimeSpan.FromMinutes(Math.Max(15, _options.RetentionMinutes));
        await _store.AppendAsync(tick, retention, cancellationToken);

        var from = tick.Timestamp - retention;
        var window = await _store.GetWindowAsync(tick.Symbol, from, cancellationToken);
        var features = _calculator.Calculate(tick.Symbol, window);
        await _store.SaveFeaturesAsync(features, cancellationToken);
        await _anomalyProcessor.ProcessAsync(tick, features, cancellationToken);

        _logger.LogInformation(
            "Computed features for {Symbol} from {EventId}: RVOL={RelativeVolume} vol1m={Volume1m} vwap={Vwap}",
            tick.Symbol,
            tick.EventId,
            features.RelativeVolume,
            features.Volume1m,
            features.Vwap);
    }
}
