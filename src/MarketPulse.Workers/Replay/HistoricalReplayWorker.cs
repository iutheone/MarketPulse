using MarketPulse.Application.Contracts;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MarketPulse.Workers.Replay;

public sealed class HistoricalReplayWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<HistoricalReplayWorker> _logger;

    public HistoricalReplayWorker(IServiceScopeFactory scopes, ILogger<HistoricalReplayWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        await using var scope = _scopes.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IMarketEventPublisher>();
        var replay = scope.ServiceProvider.GetRequiredService<IHistoricalReplayService>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<ReplayOptions>>().Value;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await publisher.EnsureInfrastructureAsync(stoppingToken);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kafka is not ready for replay. Retrying.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        _logger.LogInformation("Starting historical CSV replay of {File}", options.CsvFile);
        var result = await replay.ReplayAsync(new ReplayRequest { Source = "csv", CsvFile = options.CsvFile }, stoppingToken);
        _logger.LogInformation("Replay finished. Published {Published} of {Read} bars.", result.Published, result.BarsRead);
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
