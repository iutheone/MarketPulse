namespace MarketPulse.Application.Options;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public string OtlpEndpoint { get; set; } = string.Empty;
    public string WorkerMetricsUrl { get; set; } = "http://+:9465/";
}
