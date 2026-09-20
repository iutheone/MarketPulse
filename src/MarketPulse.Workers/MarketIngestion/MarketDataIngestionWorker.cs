using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Mapping;
using MarketPulse.Application.Options;
using MarketPulse.Domain.Events;
using MarketPulse.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace MarketPulse.Workers.MarketIngestion;

/// <summary>
/// Single worker for all symbols. Polling uses GetLatestAsync on a configurable interval;
/// Streaming uses IAsyncEnumerable from the provider. Both paths publish to the same Kafka topic.
/// </summary>
public sealed class MarketDataIngestionWorker : BackgroundService
{
    private readonly IMarketDataProvider _provider;
    private readonly IMarketEventPublisher _publisher;
    private readonly MarketDataOptions _marketData;
    private readonly SyntheticMarketDataOptions _synthetic;
    private readonly ILogger<MarketDataIngestionWorker> _logger;
    private readonly SemaphoreSlim _pollGate = new(1, 1);

    public MarketDataIngestionWorker(
        IMarketDataProvider provider,
        IMarketEventPublisher publisher,
        IOptions<MarketDataOptions> marketData,
        IOptions<SyntheticMarketDataOptions> synthetic,
        ILogger<MarketDataIngestionWorker> logger)
    {
        _provider = provider;
        _publisher = publisher;
        _marketData = marketData.Value;
        _synthetic = synthetic.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitForKafkaAsync(stoppingToken);

        var symbols = ResolveSymbols();
        _logger.LogInformation(
            "Market ingestion starting. Provider={Provider} Mode={Mode} Symbols={Symbols}",
            _marketData.Provider,
            _marketData.Mode,
            string.Join(",", symbols));

        var twelveData = _marketData.Provider.Equals("TwelveData", StringComparison.OrdinalIgnoreCase);
        if (twelveData && !_marketData.Mode.Equals("Polling", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Twelve Data REST only supports polling. Switching Mode from {Mode} to Polling.", _marketData.Mode);
        }

        if (twelveData || _marketData.Mode.Equals("Polling", StringComparison.OrdinalIgnoreCase))
        {
            await RunPollingAsync(symbols, stoppingToken);
        }
        else
        {
            await RunStreamingAsync(symbols, stoppingToken);
        }
    }

    private async Task RunStreamingAsync(IReadOnlyList<string> symbols, CancellationToken stoppingToken)
    {
        await foreach (var tick in _provider.StreamAsync(symbols, stoppingToken))
        {
            await PublishWithRetryAsync(tick, stoppingToken);
        }
    }

    private async Task RunPollingAsync(IReadOnlyList<string> symbols, CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _marketData.PollingIntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        await PollOnceAsync(symbols, stoppingToken);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PollOnceAsync(symbols, stoppingToken);
        }
    }

    private async Task PollOnceAsync(IReadOnlyList<string> symbols, CancellationToken stoppingToken)
    {
        if (!await _pollGate.WaitAsync(0, stoppingToken))
        {
            _logger.LogWarning("Skipping poll because the previous cycle is still running.");
            return;
        }

        try
        {
            var bars = await _provider.GetLatestAsync(symbols, stoppingToken);
            foreach (var bar in bars)
            {
                await PublishWithRetryAsync(MarketEventMapper.ToTick(bar), stoppingToken);
            }
        }
        finally
        {
            _pollGate.Release();
        }
    }

    private async Task PublishWithRetryAsync(MarketTick tick, CancellationToken stoppingToken)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _publisher.PublishAsync(tick, stoppingToken);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && !stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    ex,
                    "Publish failed for {EventId} {Symbol} (attempt {Attempt}/{Max}). Retrying.",
                    tick.EventId,
                    tick.Symbol,
                    attempt,
                    maxAttempts);
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), stoppingToken);
            }
        }
    }

    private async Task WaitForKafkaAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(2);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _publisher.EnsureInfrastructureAsync(stoppingToken);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kafka is not ready. Retrying in {DelaySeconds}s.", delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    private IReadOnlyList<string> ResolveSymbols()
    {
        if (_marketData.Symbols.Length > 0)
        {
            return _marketData.Symbols;
        }

        if (_marketData.Provider.Equals("Synthetic", StringComparison.OrdinalIgnoreCase)
            && _synthetic.Symbols.Length > 0)
        {
            return _synthetic.Symbols;
        }

        return ["AAPL"];
    }

    public override void Dispose()
    {
        _pollGate.Dispose();
        base.Dispose();
    }
}
