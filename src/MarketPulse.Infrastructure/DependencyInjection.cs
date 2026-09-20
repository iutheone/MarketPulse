using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Features;
using MarketPulse.Application.Options;
using MarketPulse.Domain.Features;
using MarketPulse.Domain.Interfaces;
using MarketPulse.Infrastructure.Kafka;
using MarketPulse.Infrastructure.MarketData.Synthetic;
using MarketPulse.Infrastructure.MarketData.TwelveData;
using MarketPulse.Infrastructure.Observability;
using MarketPulse.Infrastructure.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace MarketPulse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMarketPulseInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MarketDataOptions>(configuration.GetSection(MarketDataOptions.SectionName));
        services.Configure<SyntheticMarketDataOptions>(configuration.GetSection(SyntheticMarketDataOptions.SectionName));
        services.Configure<TwelveDataOptions>(configuration.GetSection(TwelveDataOptions.SectionName));
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<FeatureProcessingOptions>(configuration.GetSection(FeatureProcessingOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<IEventSerializer, JsonEventSerializer>();
        services.AddSingleton<IMarketEventPublisher, KafkaMarketEventPublisher>();
        services.AddSingleton<FeatureCalculator>();
        services.AddSingleton<IFeatureProcessor, FeatureProcessor>();
        services.AddSingleton<IMarketEventConsumer, KafkaNormalizedTickConsumer>();
        services.AddSingleton<KafkaClusterHealthCheck>();
        services.AddSingleton<RedisHealthCheck>();
        services.AddMarketDataProvider(configuration);
        services.AddRedis(configuration);

        return services;
    }

    private static void AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisOptions.ConnectionString));
        services.AddSingleton<IMarketFeatureStore, RedisMarketFeatureStore>();
    }

    private static IServiceCollection AddMarketDataProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("MarketData:Provider") ?? "Synthetic";
        switch (provider.Trim().ToLowerInvariant())
        {
            case "synthetic":
                services.AddSingleton<IMarketDataProvider, SyntheticMarketDataProvider>();
                return services;
            case "twelvedata":
                services.AddHttpClient(TwelveDataRestProvider.HttpClientName, (sp, client) =>
                {
                    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TwelveDataOptions>>().Value;
                    client.BaseAddress = new Uri(options.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds));
                    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                });
                services.AddSingleton<IMarketDataProvider, TwelveDataRestProvider>();
                return services;
            default:
                throw new InvalidOperationException(
                    $"Unknown MarketData:Provider '{provider}'. Supported: Synthetic, TwelveData.");
        }
    }
}
