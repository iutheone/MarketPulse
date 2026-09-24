using MarketPulse.Application.Interfaces;
using Microsoft.Extensions.Hosting;

namespace MarketPulse.Api.Realtime;

public sealed class AnomalyRealtimeWorker : BackgroundService
{
    private readonly IAnomalyRealtimeConsumer _consumer;

    public AnomalyRealtimeWorker(IAnomalyRealtimeConsumer consumer)
    {
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        await _consumer.StartAsync(stoppingToken);
    }
}
