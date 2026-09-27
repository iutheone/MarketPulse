using Confluent.Kafka;
using MarketPulse.Application.Alerts;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Messaging;
using MarketPulse.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketPulse.Infrastructure.Kafka;

public sealed class KafkaAlertDeliveryConsumer : IAlertDeliveryConsumer, IDisposable
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IEventSerializer _serializer;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<KafkaAlertDeliveryConsumer> _logger;

    public KafkaAlertDeliveryConsumer(
        IOptions<KafkaOptions> kafka,
        IOptions<AlertOptions> alerts,
        IEventSerializer serializer,
        IServiceScopeFactory scopes,
        ILogger<KafkaAlertDeliveryConsumer> logger)
    {
        _serializer = serializer;
        _scopes = scopes;
        _logger = logger;
        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = kafka.Value.BootstrapServers,
            GroupId = alerts.Value.DeliveryGroupId,
            ClientId = $"{kafka.Value.ClientId}-alert-delivery",
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnablePartitionEof = false
        }).Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        _consumer.Subscribe(KafkaTopics.AlertsOutbox);
        _logger.LogInformation("Alert delivery subscribed to {Topic}", KafkaTopics.AlertsOutbox);

        while (!cancellationToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result;
            try
            {
                result = await Task.Run(() => _consumer.Consume(cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (result?.Message?.Value is null)
            {
                continue;
            }

            var envelope = _serializer.Deserialize<AlertDispatch>(result.Message.Value);
            if (envelope?.Data is null)
            {
                _logger.LogWarning(
                    "Skipping unreadable alert payload at {Topic} {Partition}:{Offset}",
                    result.Topic,
                    result.Partition.Value,
                    result.Offset.Value);
                _consumer.Commit(result);
                continue;
            }

            await using var scope = _scopes.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<IAlertDeliveryProcessor>();
            await processor.ProcessAsync(envelope.Data, cancellationToken);
            _consumer.Commit(result);
        }

        _consumer.Close();
    }

    public void Dispose() => _consumer.Dispose();
}
