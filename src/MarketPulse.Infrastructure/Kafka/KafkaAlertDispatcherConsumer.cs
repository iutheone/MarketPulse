using Confluent.Kafka;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Messaging;
using MarketPulse.Application.Options;
using MarketPulse.Domain.Anomaly;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketPulse.Infrastructure.Kafka;

public sealed class KafkaAlertDispatcherConsumer : IAlertDispatchConsumer, IDisposable
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IEventSerializer _serializer;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<KafkaAlertDispatcherConsumer> _logger;

    public KafkaAlertDispatcherConsumer(
        IOptions<KafkaOptions> kafka,
        IOptions<AlertOptions> alerts,
        IEventSerializer serializer,
        IServiceScopeFactory scopes,
        ILogger<KafkaAlertDispatcherConsumer> logger)
    {
        _serializer = serializer;
        _scopes = scopes;
        _logger = logger;
        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = kafka.Value.BootstrapServers,
            GroupId = alerts.Value.DispatcherGroupId,
            ClientId = $"{kafka.Value.ClientId}-alert-dispatcher",
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnablePartitionEof = false
        }).Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        _consumer.Subscribe(KafkaTopics.MarketAnomalies);
        _logger.LogInformation("Alert dispatcher subscribed to {Topic}", KafkaTopics.MarketAnomalies);

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

            var envelope = _serializer.Deserialize<AnomalyResult>(result.Message.Value);
            if (envelope?.Data is null)
            {
                _logger.LogWarning(
                    "Skipping unreadable anomaly for alerts at {Topic} {Partition}:{Offset}",
                    result.Topic,
                    result.Partition.Value,
                    result.Offset.Value);
                _consumer.Commit(result);
                continue;
            }

            await using var scope = _scopes.CreateAsyncScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IAlertDispatcher>();
            await dispatcher.DispatchAsync(envelope.Data, cancellationToken);
            _consumer.Commit(result);
        }

        _consumer.Close();
    }

    public void Dispose() => _consumer.Dispose();
}
