using System.Text.Json;
using MarketPulse.Application.Contracts;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Options;
using MarketPulse.Domain.Features;
using MarketPulse.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace MarketPulse.Application.Replay;

public sealed class BacktestService : IBacktestService
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IHistoricalBarLoader _loader;
    private readonly FeatureCalculator _calculator;
    private readonly IAnomalyDetectionEngine _engine;
    private readonly IBacktestStore _store;
    private readonly FeatureProcessingOptions _features;

    public BacktestService(
        IHistoricalBarLoader loader,
        FeatureCalculator calculator,
        IAnomalyDetectionEngine engine,
        IBacktestStore store,
        IOptions<FeatureProcessingOptions> features)
    {
        _loader = loader;
        _calculator = calculator;
        _engine = engine;
        _store = store;
        _features = features.Value;
    }

    public async Task<BacktestRunDto> RunAsync(BacktestRequest request, CancellationToken cancellationToken)
    {
        var replayRequest = new ReplayRequest
        {
            Source = request.Source,
            CsvFile = request.CsvFile,
            Symbols = request.Symbols,
            From = request.From,
            To = request.To
        };

        var started = DateTimeOffset.UtcNow;
        try
        {
            var bars = await _loader.LoadAsync(replayRequest, cancellationToken);
            var runner = new BacktestRunner(
                _calculator,
                _engine,
                TimeSpan.FromMinutes(Math.Max(15, _features.RetentionMinutes)));
            var summaries = runner.Run(bars);
            var run = new BacktestRunDto
            {
                Id = Guid.NewGuid(),
                StartedAt = started,
                CompletedAt = DateTimeOffset.UtcNow,
                Status = "completed",
                ParametersJson = JsonSerializer.Serialize(request, Json),
                Results = summaries.Select(s => new BacktestResultDto
                {
                    Id = Guid.NewGuid(),
                    Symbol = s.Symbol,
                    Bars = s.Bars,
                    Anomalies = s.Anomalies,
                    RecordRate = s.RecordRate,
                    MeanScore = s.MeanScore,
                    Notes = BacktestNotes.Disclaimer
                }).ToList()
            };
            return await _store.SaveAsync(run, cancellationToken);
        }
        catch (Exception ex)
        {
            var failed = new BacktestRunDto
            {
                Id = Guid.NewGuid(),
                StartedAt = started,
                CompletedAt = DateTimeOffset.UtcNow,
                Status = "failed",
                ParametersJson = JsonSerializer.Serialize(request, Json),
                Results = [],
                Error = ex.Message
            };
            return await _store.SaveAsync(failed, cancellationToken);
        }
    }

    public Task<IReadOnlyList<BacktestRunDto>> ListAsync(CancellationToken cancellationToken) =>
        _store.ListAsync(cancellationToken);

    public Task<BacktestRunDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _store.GetByIdAsync(id, cancellationToken);
}
