using Confluent.Kafka;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Messaging;
using MarketPulse.Application.Options;
using MarketPulse.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketPulse.Infrastructure.Kafka;

/// <summary>
/// Consumes <c>market.normalized</c>. Offsets commit only after feature processing succeeds.
/// </summary>
public sealed class KafkaNormalizedTickConsumer : IMarketEventConsumer, IDisposable
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IEventSerializer _serializer;
    private readonly IFeatureProcessor _processor;
    private readonly ILogger<KafkaNormalizedTickConsumer> _logger;

    public KafkaNormalizedTickConsumer(
        IOptions<KafkaOptions> options,
        IEventSerializer serializer,
        IFeatureProcessor processor,
        ILogger<KafkaNormalizedTickConsumer> logger)
    {
        _serializer = serializer;
        _processor = processor;
        _logger = logger;
        var kafka = options.Value;
        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = kafka.BootstrapServers,
            GroupId = kafka.FeatureProcessorGroupId,
            ClientId = $"{kafka.ClientId}-feature-consumer",
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnablePartitionEof = false
        }).Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _consumer.Subscribe(KafkaTopics.MarketNormalized);
        _logger.LogInformation("Feature processor subscribed to {Topic}", KafkaTopics.MarketNormalized);

        while (!cancellationToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result;
            try
            {
                result = _consumer.Consume(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (result?.Message?.Value is null)
            {
                continue;
            }

            var envelope = _serializer.Deserialize<MarketTick>(result.Message.Value);
            if (envelope?.Data is null)
            {
                _logger.LogWarning(
                    "Skipping unreadable payload at {Topic} {Partition}:{Offset}",
                    result.Topic,
                    result.Partition.Value,
                    result.Offset.Value);
                _consumer.Commit(result);
                continue;
            }

            await _processor.ProcessAsync(envelope.Data, cancellationToken);
            _consumer.Commit(result);
        }

        _consumer.Close();
    }

    public void Dispose()
    {
        _consumer.Dispose();
    }
}
