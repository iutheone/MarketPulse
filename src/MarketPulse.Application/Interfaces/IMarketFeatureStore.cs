using MarketPulse.Domain.Events;
using MarketPulse.Domain.Features;

namespace MarketPulse.Application.Interfaces;

/// <summary>
/// Hot-state store for rolling windows and latest features. Redis implements this; PostgreSQL does not.
/// </summary>
public interface IMarketFeatureStore
{
    Task AppendAsync(MarketTick tick, TimeSpan retention, CancellationToken cancellationToken);

    Task<IReadOnlyList<TickSample>> GetWindowAsync(
        string symbol,
        DateTimeOffset fromInclusive,
        CancellationToken cancellationToken);

    Task SaveFeaturesAsync(MarketFeatures features, CancellationToken cancellationToken);

    Task<MarketFeatures?> GetFeaturesAsync(string symbol, CancellationToken cancellationToken);
}
