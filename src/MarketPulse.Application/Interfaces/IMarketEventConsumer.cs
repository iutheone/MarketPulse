namespace MarketPulse.Application.Interfaces;

/// <summary>
/// Inbound Kafka consumer port. Phase 1 does not start consumers; Feature Processor will implement this.
/// </summary>
public interface IMarketEventConsumer
{
    Task StartAsync(CancellationToken cancellationToken);
}
