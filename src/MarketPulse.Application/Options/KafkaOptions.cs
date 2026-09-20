namespace MarketPulse.Application.Options;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ClientId { get; set; } = "marketpulse";
    public int NormalizedTopicPartitions { get; set; } = 6;
    public short ReplicationFactor { get; set; } = 1;
    public string FeatureProcessorGroupId { get; set; } = "marketpulse-feature-processor";
}
