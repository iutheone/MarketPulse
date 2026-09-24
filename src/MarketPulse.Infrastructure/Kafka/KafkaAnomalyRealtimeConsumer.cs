using Confluent.Kafka;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Messaging;
using MarketPulse.Application.Options;
using MarketPulse.Domain.Anomaly;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketPulse.Infrastructure.Kafka;

/// <summary>
/// API-side consumer of <c>market.anomalies</c>. Fan-out is SignalR, not persistence.
/// Offsets commit after the hub send succeeds. Replay history with REST, not this consumer.
/// </summary>
public sealed class KafkaAnomalyRealtimeConsumer : IAnomalyRealtimeConsumer, IDisposable
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IEventSerializer _serializer;
    private readonly IAnomalyRealtimePublisher _publisher;
    private readonly ILogger<KafkaAnomalyRealtimeConsumer> _logger;

    public KafkaAnomalyRealtimeConsumer(
        IOptions<KafkaOptions> options,
        IEventSerializer serializer,
        IAnomalyRealtimePublisher publisher,
        ILogger<KafkaAnomalyRealtimeConsumer> logger)
    {
        _serializer = serializer;
        _publisher = publisher;
        _logger = logger;
        var kafka = options.Value;
        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = kafka.BootstrapServers,
            GroupId = kafka.RealtimeGroupId,
            ClientId = $"{kafka.ClientId}-anomaly-realtime",
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnablePartitionEof = false
        }).Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        _consumer.Subscribe(KafkaTopics.MarketAnomalies);
        _logger.LogInformation("Realtime consumer subscribed to {Topic}", KafkaTopics.MarketAnomalies);

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
                    "Skipping unreadable anomaly payload at {Topic} {Partition}:{Offset}",
                    result.Topic,
                    result.Partition.Value,
                    result.Offset.Value);
                _consumer.Commit(result);
                continue;
            }

            await _publisher.PublishAsync(envelope.Data, cancellationToken);
            _consumer.Commit(result);
        }

        _consumer.Close();
    }

    public void Dispose()
    {
        _consumer.Dispose();
    }
}
