using MarketPulse.Application.Interfaces;

namespace MarketPulse.Workers.Alerts;

public sealed class AlertDeliveryWorker : BackgroundService
{
    private readonly IMarketEventPublisher _publisher;
    private readonly IAlertDeliveryConsumer _consumer;
    private readonly ILogger<AlertDeliveryWorker> _logger;

    public AlertDeliveryWorker(
        IMarketEventPublisher publisher,
        IAlertDeliveryConsumer consumer,
        ILogger<AlertDeliveryWorker> logger)
    {
        _publisher = publisher;
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitForKafkaAsync(stoppingToken);
        _logger.LogInformation("Alert delivery starting.");
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
                _logger.LogWarning(ex, "Kafka is not ready for alert delivery. Retrying in {DelaySeconds}s.", delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
