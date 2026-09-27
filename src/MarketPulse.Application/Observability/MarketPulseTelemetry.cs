using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MarketPulse.Application.Observability;

public static class MarketPulseTelemetry
{
    public const string MeterName = "MarketPulse";
    public const string ActivitySourceName = "MarketPulse";

    public static readonly Meter Meter = new(MeterName, "1.0.0");
    public static readonly ActivitySource Activity = new(ActivitySourceName);

    public static readonly Counter<long> TicksPublished = Meter.CreateCounter<long>(
        "marketpulse.ticks.published",
        description: "Canonical ticks published to market.normalized");

    public static readonly Counter<long> TicksProcessed = Meter.CreateCounter<long>(
        "marketpulse.ticks.processed",
        description: "Ticks that completed feature processing");

    public static readonly Counter<long> AnomaliesRecorded = Meter.CreateCounter<long>(
        "marketpulse.anomalies.recorded",
        description: "Anomalies persisted and published (not a trade count)");

    public static readonly Counter<long> AlertsDispatched = Meter.CreateCounter<long>(
        "marketpulse.alerts.dispatched",
        description: "Alert jobs written to alerts.outbox");

    public static readonly Counter<long> AlertsDelivered = Meter.CreateCounter<long>(
        "marketpulse.alerts.delivered",
        description: "Webhook deliveries that returned 2xx");

    public static readonly Counter<long> AlertsFailed = Meter.CreateCounter<long>(
        "marketpulse.alerts.failed",
        description: "Webhook deliveries sent to the DLQ");

    public static readonly Histogram<double> FeatureDurationMs = Meter.CreateHistogram<double>(
        "marketpulse.features.duration",
        unit: "ms",
        description: "Feature + anomaly processing time");
}
