using MarketPulse.Application.Interfaces;

namespace MarketPulse.Workers.FeatureProcessor;

public sealed class FeatureProcessorWorker : BackgroundService
{
    private readonly IMarketEventPublisher _publisher;
    private readonly IMarketEventConsumer _consumer;
    private readonly ILogger<FeatureProcessorWorker> _logger;

    public FeatureProcessorWorker(
        IMarketEventPublisher publisher,
        IMarketEventConsumer consumer,
        ILogger<FeatureProcessorWorker> logger)
    {
        _publisher = publisher;
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitForKafkaAsync(stoppingToken);
        _logger.LogInformation("Feature processor starting.");
        await _consumer.StartAsync(stoppingToken);
    }

    private async Task WaitForKafkaAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(2);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _publisher.EnsureInfrastructureAsync(stoppingToken);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kafka is not ready for feature processor. Retrying in {DelaySeconds}s.", delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
