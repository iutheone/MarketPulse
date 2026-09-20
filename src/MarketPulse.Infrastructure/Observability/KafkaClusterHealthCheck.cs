using Confluent.Kafka;
using MarketPulse.Application.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace MarketPulse.Infrastructure.Observability;

public sealed class KafkaClusterHealthCheck : IHealthCheck
{
    private readonly KafkaOptions _options;

    public KafkaClusterHealthCheck(IOptions<KafkaOptions> options)
    {
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var admin = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = _options.BootstrapServers,
                SocketTimeoutMs = 2000
            }).Build();

            var metadata = admin.GetMetadata(TimeSpan.FromSeconds(2));
            if (metadata.Brokers.Count == 0)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Kafka returned no brokers."));
            }

            return Task.FromResult(HealthCheckResult.Healthy($"brokers={metadata.Brokers.Count}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Kafka metadata call failed.", ex));
        }
    }
}
