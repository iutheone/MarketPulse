using Confluent.Kafka;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Mapping;
using MarketPulse.Application.Messaging;
using MarketPulse.Application.Options;
using MarketPulse.Domain.Anomaly;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketPulse.Infrastructure.Kafka;

public sealed class KafkaAnomalyEventPublisher : IAnomalyEventPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly IEventSerializer _serializer;
    private readonly ILogger<KafkaAnomalyEventPublisher> _logger;

    public KafkaAnomalyEventPublisher(
        IOptions<KafkaOptions> options,
        IEventSerializer serializer,
        ILogger<KafkaAnomalyEventPublisher> logger)
    {
        _serializer = serializer;
        _logger = logger;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            ClientId = $"{options.Value.ClientId}-anomaly-producer",
            Acks = Acks.All,
            EnableIdempotence = true,
            LingerMs = 5,
            CompressionType = CompressionType.Lz4
        }).Build();
    }

    public async Task PublishAsync(AnomalyResult anomaly, CancellationToken cancellationToken)
    {
        var envelope = AnomalyEventMapper.ToEnvelope(anomaly);
        var payload = _serializer.Serialize(envelope);
        var result = await _producer.ProduceAsync(
            KafkaTopics.MarketAnomalies,
            new Message<string, string>
            {
                Key = anomaly.Symbol,
                Value = payload,
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(envelope.EventType) },
                    { "schema-version", System.Text.Encoding.UTF8.GetBytes(envelope.SchemaVersion.ToString()) }
                }
            },
            cancellationToken);

        _logger.LogInformation(
            "Published anomaly {AnomalyId} for {Symbol} to {Topic} partition {Partition} offset {Offset}",
            anomaly.AnomalyId,
            anomaly.Symbol,
            result.Topic,
            result.Partition.Value,
            result.Offset.Value);
    }
}
