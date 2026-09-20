using MarketPulse.Domain.Events;

namespace MarketPulse.Application.Interfaces;

public interface IFeatureProcessor
{
    Task ProcessAsync(MarketTick tick, CancellationToken cancellationToken);
}
