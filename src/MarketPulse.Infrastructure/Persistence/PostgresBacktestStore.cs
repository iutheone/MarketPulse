using System.Text.Json;
using MarketPulse.Application.Contracts;
using MarketPulse.Application.Interfaces;
using MarketPulse.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketPulse.Infrastructure.Persistence;

public sealed class PostgresBacktestStore : IBacktestStore
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly MarketPulseDbContext _db;

    public PostgresBacktestStore(MarketPulseDbContext db)
    {
        _db = db;
    }

    public async Task<BacktestRunDto> SaveAsync(BacktestRunDto run, CancellationToken cancellationToken)
    {
        var row = new BacktestRunRecord
        {
            Id = run.Id,
            StartedAt = run.StartedAt,
            Status = run.Status,
            ParametersJson = run.ParametersJson
        };
        _db.BacktestRuns.Add(row);
        foreach (var result in run.Results)
        {
            _db.BacktestResults.Add(new BacktestResultRecord
            {
                Id = result.Id,
                RunId = run.Id,
                Symbol = result.Symbol,
                HitRate = result.RecordRate,
                Notes = JsonSerializer.Serialize(new StoredNotes(result.Bars, result.Anomalies, result.MeanScore, result.Notes), Json)
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return run;
    }

    public async Task<IReadOnlyList<BacktestRunDto>> ListAsync(CancellationToken cancellationToken)
    {
        var runs = await _db.BacktestRuns.AsNoTracking()
            .OrderByDescending(r => r.StartedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
        var ids = runs.Select(r => r.Id).ToList();
        var results = await _db.BacktestResults.AsNoTracking()
            .Where(r => ids.Contains(r.RunId))
            .ToListAsync(cancellationToken);
        return runs.Select(run => Map(run, results.Where(r => r.RunId == run.Id).ToList())).ToList();
    }

    public async Task<BacktestRunDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var run = await _db.BacktestRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (run is null)
        {
            return null;
        }

        var results = await _db.BacktestResults.AsNoTracking().Where(r => r.RunId == id).ToListAsync(cancellationToken);
        return Map(run, results);
    }

    private static BacktestRunDto Map(BacktestRunRecord run, IReadOnlyList<BacktestResultRecord> results)
    {
        return new BacktestRunDto
        {
            Id = run.Id,
            StartedAt = run.StartedAt,
            CompletedAt = run.Status == "completed" ? run.StartedAt : null,
            Status = run.Status,
            ParametersJson = run.ParametersJson,
            Results = results.Select(MapResult).ToList()
        };
    }

    private static BacktestResultDto MapResult(BacktestResultRecord row)
    {
        StoredNotes? notes = null;
        try
        {
            notes = JsonSerializer.Deserialize<StoredNotes>(row.Notes ?? "{}", Json);
        }
        catch (JsonException)
        {
            // older rows
        }

        return new BacktestResultDto
        {
            Id = row.Id,
            Symbol = row.Symbol,
            Bars = notes?.Bars ?? 0,
            Anomalies = notes?.Anomalies ?? 0,
            RecordRate = row.HitRate ?? 0,
            MeanScore = notes?.MeanScore,
            Notes = notes?.Disclaimer ?? row.Notes ?? ""
        };
    }

    private sealed record StoredNotes(int Bars, int Anomalies, decimal? MeanScore, string Disclaimer);
}
