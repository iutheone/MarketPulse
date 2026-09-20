namespace MarketPulse.Application.Messaging;

public static class KafkaTopics
{
    public const string MarketNormalized = "market.normalized";
    public const string MarketAnomalies = "market.anomalies";
    public const string AlertsOutbox = "alerts.outbox";
    public const string AlertsDlq = "alerts.dlq";
}

public static class EventTypes
{
    public const string MarketTickNormalized = "market.tick.normalized";
}
