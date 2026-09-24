using MarketPulse.Application.Anomaly;
using MarketPulse.Application.Features;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Options;
using MarketPulse.Application.Replay;
using MarketPulse.Domain.Anomaly;
using MarketPulse.Domain.Features;
using MarketPulse.Domain.Interfaces;
using MarketPulse.Infrastructure.Kafka;
using MarketPulse.Infrastructure.MarketData.Replay;
using MarketPulse.Infrastructure.MarketData.Synthetic;
using MarketPulse.Infrastructure.MarketData.TwelveData;
using MarketPulse.Infrastructure.Observability;
using MarketPulse.Infrastructure.Persistence;
using MarketPulse.Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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
        services.Configure<AnomalyDetectionOptions>(configuration.GetSection(AnomalyDetectionOptions.SectionName));
        services.Configure<PostgresOptions>(configuration.GetSection(PostgresOptions.SectionName));
        services.Configure<SignalROptions>(configuration.GetSection(SignalROptions.SectionName));
        services.Configure<ReplayOptions>(configuration.GetSection(ReplayOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<IEventSerializer, JsonEventSerializer>();
        services.AddSingleton<IMarketEventPublisher, KafkaMarketEventPublisher>();
        services.AddSingleton<IAnomalyEventPublisher, KafkaAnomalyEventPublisher>();
        services.AddSingleton<FeatureCalculator>();
        services.AddSingleton<IAnomalyDetectionEngine>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AnomalyDetectionOptions>>().Value;
            return new AnomalyDetectionEngine(options.ToParameters());
        });
        services.AddScoped<IAnomalyProcessor, AnomalyProcessor>();
        services.AddScoped<IFeatureProcessor, FeatureProcessor>();
        services.AddSingleton<IMarketEventConsumer, KafkaNormalizedTickConsumer>();
        services.AddSingleton<KafkaClusterHealthCheck>();
        services.AddSingleton<RedisHealthCheck>();
        services.AddSingleton<PostgresHealthCheck>();
        services.AddMarketDataProvider(configuration);
        services.AddRedis(configuration);
        services.AddPostgres(configuration);

        return services;
    }

    public static async Task InitializeMarketPulseDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketPulseDbContext>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AnomalyDetectionOptions>>().Value;
        await MarketPulseDatabase.InitializeAsync(db, options, cancellationToken);
    }

    private static void AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisOptions.ConnectionString));
        services.AddSingleton<IMarketFeatureStore, RedisMarketFeatureStore>();
        services.AddSingleton<IAnomalyFeed, RedisAnomalyFeed>();
    }

    private static void AddPostgres(this IServiceCollection services, IConfiguration configuration)
    {
        var postgres = configuration.GetSection(PostgresOptions.SectionName).Get<PostgresOptions>() ?? new PostgresOptions();
        services.AddDbContext<MarketPulseDbContext>(options =>
            options.UseNpgsql(postgres.ConnectionString, npgsql => npgsql.EnableRetryOnFailure(3)));
        services.AddScoped<IAnomalyStore, PostgresAnomalyStore>();
        services.AddScoped<PostgresCatalogStore>();
        services.AddScoped<IBarHistoryStore>(sp => sp.GetRequiredService<PostgresCatalogStore>());
        services.AddScoped<IWatchlistStore>(sp => sp.GetRequiredService<PostgresCatalogStore>());
        services.AddScoped<IDetectionRuleStore>(sp => sp.GetRequiredService<PostgresCatalogStore>());
        services.AddScoped<IHistoricalBarLoader, MarketData.Replay.HistoricalBarLoader>();
        services.AddScoped<IHistoricalReplayService, HistoricalReplayService>();
        services.AddScoped<IBacktestStore, PostgresBacktestStore>();
        services.AddScoped<IBacktestService, BacktestService>();
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
                    var options = sp.GetRequiredService<IOptions<TwelveDataOptions>>().Value;
                    client.BaseAddress = new Uri(options.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds));
                    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                });
                services.AddSingleton<IMarketDataProvider, TwelveDataRestProvider>();
                return services;
            case "replay":
                services.AddSingleton<IMarketDataProvider, DisabledMarketDataProvider>();
                return services;
            default:
                throw new InvalidOperationException(
                    $"Unknown MarketData:Provider '{provider}'. Supported: Synthetic, TwelveData, Replay.");
        }
    }
}
