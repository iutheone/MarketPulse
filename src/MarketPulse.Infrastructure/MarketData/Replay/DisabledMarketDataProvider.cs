using MarketPulse.Domain.Entities;
using MarketPulse.Domain.Events;
using MarketPulse.Domain.Interfaces;

namespace MarketPulse.Infrastructure.MarketData.Replay;

/// <summary>
/// Placeholder so DI still has an IMarketDataProvider when MarketData:Provider is Replay.
/// Replay ticks come from HistoricalReplayService, not this type.
/// </summary>
public sealed class DisabledMarketDataProvider : IMarketDataProvider
{
    public Task<IReadOnlyList<MarketBar>> GetLatestAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MarketBar>>([]);

    public async IAsyncEnumerable<MarketTick> StreamAsync(
        IEnumerable<string> symbols,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        yield break;
    }
}
