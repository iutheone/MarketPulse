using Confluent.Kafka;
using MarketPulse.Application.Alerts;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Messaging;
using MarketPulse.Application.Options;
using MarketPulse.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketPulse.Infrastructure.Kafka;

public sealed class KafkaAlertOutboxPublisher : IAlertOutboxPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly IEventSerializer _serializer;
    private readonly ILogger<KafkaAlertOutboxPublisher> _logger;

    public KafkaAlertOutboxPublisher(
        IOptions<KafkaOptions> options,
        IEventSerializer serializer,
        ILogger<KafkaAlertOutboxPublisher> logger)
    {
        _serializer = serializer;
        _logger = logger;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            ClientId = $"{options.Value.ClientId}-alert-outbox",
            Acks = Acks.All,
            EnableIdempotence = true,
            LingerMs = 5,
            CompressionType = CompressionType.Lz4
        }).Build();
    }

    public Task PublishAsync(AlertDispatch dispatch, CancellationToken cancellationToken) =>
        ProduceAsync(
            KafkaTopics.AlertsOutbox,
            EventTypes.AlertDispatch,
            SchemaVersions.Alert,
            dispatch.DeliveryId,
            dispatch.Anomaly.Symbol,
            dispatch.Anomaly.DetectedAt,
            dispatch,
            cancellationToken);

    public Task PublishDeadLetterAsync(AlertDispatch dispatch, string error, CancellationToken cancellationToken) =>
        ProduceAsync(
            KafkaTopics.AlertsDlq,
            EventTypes.AlertDeadLetter,
            SchemaVersions.Alert,
            dispatch.DeliveryId,
            dispatch.Anomaly.Symbol,
            DateTimeOffset.UtcNow,
            new AlertDeadLetter { Dispatch = dispatch, Error = error },
            cancellationToken);

    private async Task ProduceAsync<T>(
        string topic,
        string eventType,
        int schemaVersion,
        Guid eventId,
        string key,
        DateTimeOffset occurredAt,
        T data,
        CancellationToken cancellationToken)
    {
        var envelope = new EventEnvelope<T>
        {
            EventId = eventId,
            EventType = eventType,
            SchemaVersion = schemaVersion,
            OccurredAt = occurredAt,
            Data = data
        };
        var result = await _producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = key,
                Value = _serializer.Serialize(envelope),
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(eventType) }
                }
            },
            cancellationToken);

        _logger.LogInformation(
            "Published {EventType} {EventId} to {Topic} partition {Partition} offset {Offset}",
            eventType,
            eventId,
            result.Topic,
            result.Partition.Value,
            result.Offset.Value);
    }
}
