using Confluent.Kafka;
using Confluent.Kafka.Admin;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Mapping;
using MarketPulse.Application.Messaging;
using MarketPulse.Application.Options;
using MarketPulse.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketPulse.Infrastructure.Kafka;

/// <summary>
/// Publishes <see cref="MarketTick"/> envelopes to Kafka keyed by symbol so a symbol's events
/// stay on one partition (ordering within that symbol). Domain code never references Confluent.Kafka.
/// </summary>
public sealed class KafkaMarketEventPublisher : IMarketEventPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly IEventSerializer _serializer;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaMarketEventPublisher> _logger;

    public KafkaMarketEventPublisher(
        IOptions<KafkaOptions> options,
        IEventSerializer serializer,
        ILogger<KafkaMarketEventPublisher> logger)
    {
        _options = options.Value;
        _serializer = serializer;
        _logger = logger;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            ClientId = $"{_options.ClientId}-normalized-producer",
            Acks = Acks.All,
            EnableIdempotence = true,
            LingerMs = 5,
            CompressionType = CompressionType.Lz4
        }).Build();
    }

    public async Task EnsureInfrastructureAsync(CancellationToken cancellationToken)
    {
        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = _options.BootstrapServers
        }).Build();

        var topics = new[]
        {
            KafkaTopics.MarketNormalized,
            KafkaTopics.MarketAnomalies,
            KafkaTopics.AlertsOutbox,
            KafkaTopics.AlertsDlq
        };

        try
        {
            await admin.CreateTopicsAsync(topics.Select(name => new TopicSpecification
            {
                Name = name,
                NumPartitions = _options.NormalizedTopicPartitions,
                ReplicationFactor = _options.ReplicationFactor
            }));
        }
        catch (CreateTopicsException ex)
        {
            var unexpected = ex.Results.Where(r => r.Error.Code != ErrorCode.TopicAlreadyExists && r.Error.IsError).ToList();
            if (unexpected.Count > 0)
            {
                throw;
            }
        }

        _logger.LogInformation(
            "Kafka topics ensured on {BootstrapServers}: {Topics}",
            _options.BootstrapServers,
            string.Join(", ", topics));
    }

    public async Task PublishAsync(MarketTick marketTick, CancellationToken cancellationToken)
    {
        var envelope = MarketEventMapper.ToEnvelope(marketTick);
        var payload = _serializer.Serialize(envelope);

        var result = await _producer.ProduceAsync(
            KafkaTopics.MarketNormalized,
            new Message<string, string>
            {
                Key = marketTick.Symbol,
                Value = payload,
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(envelope.EventType) },
                    { "schema-version", System.Text.Encoding.UTF8.GetBytes(envelope.SchemaVersion.ToString()) }
                }
            },
            cancellationToken);

        _logger.LogInformation(
            "Published market event {EventId} for {Symbol} to {Topic} partition {Partition} offset {Offset}",
            marketTick.EventId,
            marketTick.Symbol,
            result.Topic,
            result.Partition.Value,
            result.Offset.Value);
    }

    public ValueTask DisposeAsync()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
        return ValueTask.CompletedTask;
    }
}
