using MarketPulse.Domain.Entities;
using MarketPulse.Domain.Events;

namespace MarketPulse.Domain.Interfaces;

/// <summary>
/// Market-data source port. Implementations (synthetic, REST, WebSocket, replay)
/// live in Infrastructure. Downstream Kafka publishing must not know which provider produced a tick.
/// </summary>
public interface IMarketDataProvider
{
    Task<IReadOnlyList<MarketBar>> GetLatestAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken);

    IAsyncEnumerable<MarketTick> StreamAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken);
}
