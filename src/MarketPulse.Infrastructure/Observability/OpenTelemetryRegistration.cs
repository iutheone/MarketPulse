using MarketPulse.Application.Observability;
using MarketPulse.Application.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MarketPulse.Infrastructure.Observability;

public static class OpenTelemetryRegistration
{
    public static IServiceCollection AddMarketPulseWorkerTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>()
            ?? new ObservabilityOptions();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("marketpulse-workers"))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(MarketPulseTelemetry.MeterName)
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddPrometheusHttpListener(listener =>
                    {
                        listener.UriPrefixes =
                        [
                            string.IsNullOrWhiteSpace(options.WorkerMetricsUrl)
                                ? "http://+:9465/"
                                : options.WorkerMetricsUrl
                        ];
                    });
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(MarketPulseTelemetry.ActivitySourceName)
                    .AddHttpClientInstrumentation();
                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(otlp => otlp.Endpoint = new Uri(options.OtlpEndpoint));
                }
            });

        return services;
    }
}
