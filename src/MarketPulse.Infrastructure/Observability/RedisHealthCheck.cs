using MarketPulse.Application.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace MarketPulse.Infrastructure.Observability;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisOptions _options;

    public RedisHealthCheck(IConnectionMultiplexer redis, IOptions<RedisOptions> options)
    {
        _redis = redis;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var pong = await _redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"redis ping {pong.TotalMilliseconds:F0}ms");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Redis unreachable ({_options.ConnectionString.Split(',')[0]}).", ex);
        }
    }
}
