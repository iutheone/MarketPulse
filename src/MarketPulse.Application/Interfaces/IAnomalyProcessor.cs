using MarketPulse.Domain.Anomaly;
using MarketPulse.Domain.Events;
using MarketPulse.Domain.Features;
using MarketPulse.Application.Queries;

namespace MarketPulse.Application.Interfaces;

public interface IAnomalyProcessor
{
    Task ProcessAsync(MarketTick tick, MarketFeatures features, CancellationToken cancellationToken);
}

public interface IAnomalyStore
{
    Task<bool> TryRecordAsync(MarketTick tick, AnomalyResult? anomaly, CancellationToken cancellationToken);

    Task<AnomalyResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<AnomalyResult?> GetLatestBySymbolAsync(string symbol, CancellationToken cancellationToken);

    Task<(IReadOnlyList<AnomalyResult> Items, int Total)> SearchAsync(AnomalyQuery query, CancellationToken cancellationToken);
}

public interface IAnomalyEventPublisher
{
    Task PublishAsync(AnomalyResult anomaly, CancellationToken cancellationToken);
}

public interface IAnomalyFeed
{
    Task PushAsync(AnomalyResult anomaly, CancellationToken cancellationToken);
}

public interface IAnomalyRealtimePublisher
{
    Task PublishAsync(AnomalyResult anomaly, CancellationToken cancellationToken);
}

public interface IAnomalyRealtimeConsumer
{
    Task StartAsync(CancellationToken cancellationToken);
}
