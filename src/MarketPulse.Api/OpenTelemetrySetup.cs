using MarketPulse.Application.Observability;
using MarketPulse.Application.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

public static class OpenTelemetrySetup
{
    public static WebApplicationBuilder AddMarketPulseApiTelemetry(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>()
            ?? new ObservabilityOptions();

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("marketpulse-api"))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(MarketPulseTelemetry.MeterName)
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddPrometheusExporter();
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(MarketPulseTelemetry.ActivitySourceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(otlp => otlp.Endpoint = new Uri(options.OtlpEndpoint));
                }
            });

        return builder;
    }
}
