using MarketPulse.Application.Contracts;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Mapping;
using MarketPulse.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketPulse.Application.Replay;

public sealed class HistoricalReplayService : IHistoricalReplayService
{
    private readonly IHistoricalBarLoader _loader;
    private readonly IMarketEventPublisher _publisher;
    private readonly ReplayOptions _options;
    private readonly ILogger<HistoricalReplayService> _logger;

    public HistoricalReplayService(
        IHistoricalBarLoader loader,
        IMarketEventPublisher publisher,
        IOptions<ReplayOptions> options,
        ILogger<HistoricalReplayService> logger)
    {
        _loader = loader;
        _publisher = publisher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ReplayResultDto> ReplayAsync(ReplayRequest request, CancellationToken cancellationToken)
    {
        var bars = await _loader.LoadAsync(request, cancellationToken);
        var delay = TimeSpan.FromMilliseconds(Math.Max(0, request.DelayMilliseconds > 0 ? request.DelayMilliseconds : _options.DelayMilliseconds));
        var published = 0;

        foreach (var bar in bars)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _publisher.PublishAsync(MarketEventMapper.ToTick(bar), cancellationToken);
            published++;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);
            }
        }

        _logger.LogInformation(
            "Replay published {Published} ticks from {Source} onto market.normalized. Downstream does not know the origin.",
            published,
            request.Source);

        return new ReplayResultDto
        {
            BarsRead = bars.Count,
            Published = published,
            Source = request.Source,
            Note = "Ticks went to market.normalized. Feature/anomaly processors treat them like live data. Not trading advice."
        };
    }
}
