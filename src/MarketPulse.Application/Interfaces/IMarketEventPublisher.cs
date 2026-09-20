using MarketPulse.Domain.Events;

namespace MarketPulse.Application.Interfaces;

/// <summary>
/// Outbound port for normalized market events. Kafka is one implementation; tests can substitute an in-memory publisher.
/// </summary>
public interface IMarketEventPublisher : IAsyncDisposable
{
    Task EnsureInfrastructureAsync(CancellationToken cancellationToken);

    Task PublishAsync(MarketTick marketTick, CancellationToken cancellationToken);
}
