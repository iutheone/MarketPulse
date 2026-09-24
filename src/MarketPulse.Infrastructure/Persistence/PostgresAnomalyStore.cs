using System.Text.Json;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Options;
using MarketPulse.Application.Queries;
using MarketPulse.Domain.Anomaly;
using MarketPulse.Domain.Enums;
using MarketPulse.Domain.Events;
using MarketPulse.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MarketPulse.Infrastructure.Persistence;

public sealed class PostgresAnomalyStore : IAnomalyStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly MarketPulseDbContext _db;

    public PostgresAnomalyStore(MarketPulseDbContext db)
    {
        _db = db;
    }

    public async Task<bool> TryRecordAsync(MarketTick tick, AnomalyResult? anomaly, CancellationToken cancellationToken)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _db.ProcessedEvents.Add(new ProcessedEventRecord
            {
                EventId = tick.EventId,
                Symbol = tick.Symbol,
                ProcessedAt = DateTimeOffset.UtcNow
            });

            _db.MarketEvents.Add(new MarketEventRecord
            {
                EventId = tick.EventId,
                Symbol = tick.Symbol,
                Timestamp = tick.Timestamp,
                Source = tick.Source,
                SchemaVersion = tick.SchemaVersion,
                PayloadJson = JsonSerializer.Serialize(tick, Json)
            });

            _db.MarketBars.Add(new MarketBarRecord
            {
                EventId = tick.EventId,
                Symbol = tick.Symbol,
                Timestamp = tick.Timestamp,
                Open = tick.Open,
                High = tick.High,
                Low = tick.Low,
                Close = tick.Close,
                Volume = tick.Volume,
                Source = tick.Source
            });

            if (anomaly is not null)
            {
                _db.Anomalies.Add(ToRecord(anomaly));
            }

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await tx.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<AnomalyResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await _db.Anomalies.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        return row is null ? null : FromRecord(row);
    }

    public async Task<AnomalyResult?> GetLatestBySymbolAsync(string symbol, CancellationToken cancellationToken)
    {
        var row = await _db.Anomalies.AsNoTracking()
            .Where(a => a.Symbol == symbol)
            .OrderByDescending(a => a.DetectedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : FromRecord(row);
    }

    public async Task<(IReadOnlyList<AnomalyResult> Items, int Total)> SearchAsync(
        AnomalyQuery query,
        CancellationToken cancellationToken)
    {
        var rowsQuery = _db.Anomalies.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Symbol))
        {
            rowsQuery = rowsQuery.Where(a => a.Symbol == query.Symbol);
        }

        if (!string.IsNullOrWhiteSpace(query.Severity))
        {
            rowsQuery = rowsQuery.Where(a => a.Severity == query.Severity);
        }

        rowsQuery = (query.Sort, query.Direction) switch
        {
            ("score", "asc") => rowsQuery.OrderBy(a => a.Score),
            ("score", _) => rowsQuery.OrderByDescending(a => a.Score),
            ("symbol", "asc") => rowsQuery.OrderBy(a => a.Symbol),
            ("symbol", _) => rowsQuery.OrderByDescending(a => a.Symbol),
            ("severity", "asc") => rowsQuery.OrderBy(a => a.Severity),
            ("severity", _) => rowsQuery.OrderByDescending(a => a.Severity),
            (_, "asc") => rowsQuery.OrderBy(a => a.DetectedAt),
            _ => rowsQuery.OrderByDescending(a => a.DetectedAt)
        };

        var total = await rowsQuery.CountAsync(cancellationToken);
        var rows = await rowsQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (rows.Select(FromRecord).ToList(), total);
    }

    internal static AnomalyRecord ToRecord(AnomalyResult anomaly) => new()
    {
        Id = anomaly.AnomalyId,
        SourceEventId = anomaly.SourceEventId,
        Symbol = anomaly.Symbol,
        DetectedAt = anomaly.DetectedAt,
        Score = anomaly.Score,
        Severity = anomaly.Severity.ToString(),
        ReasonsJson = JsonSerializer.Serialize(anomaly.Reasons, Json),
        RuleVersion = anomaly.RuleVersion,
        RelativeVolume = anomaly.RelativeVolume,
        VolumeAcceleration = anomaly.VolumeAcceleration,
        PriceChangePercent = anomaly.PriceChangePercent,
        VwapDeviationPercent = anomaly.VwapDeviationPercent,
        Breakout = anomaly.Breakout,
        LastPrice = anomaly.LastPrice,
        Volume1m = anomaly.Volume1m
    };

    internal static AnomalyResult FromRecord(AnomalyRecord row)
    {
        var reasons = JsonSerializer.Deserialize<List<string>>(row.ReasonsJson, Json) ?? [];
        Enum.TryParse<AnomalySeverity>(row.Severity, out var severity);
        return new AnomalyResult
        {
            AnomalyId = row.Id,
            SourceEventId = row.SourceEventId,
            Symbol = row.Symbol,
            DetectedAt = row.DetectedAt,
            Score = row.Score,
            Severity = severity,
            Reasons = reasons,
            RuleVersion = row.RuleVersion,
            RelativeVolume = row.RelativeVolume,
            VolumeAcceleration = row.VolumeAcceleration,
            PriceChangePercent = row.PriceChangePercent,
            VwapDeviationPercent = row.VwapDeviationPercent,
            Breakout = row.Breakout,
            LastPrice = row.LastPrice,
            Volume1m = row.Volume1m
        };
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}

public static class MarketPulseDatabase
{
    public static async Task InitializeAsync(MarketPulseDbContext db, AnomalyDetectionOptions options, CancellationToken cancellationToken)
    {
        await db.Database.MigrateAsync(cancellationToken);

        if (await db.DetectionRules.AnyAsync(r => r.Version == options.RuleVersion, cancellationToken))
        {
            return;
        }

        db.DetectionRules.Add(new DetectionRuleRecord
        {
            Id = Guid.NewGuid(),
            Version = options.RuleVersion,
            IsActive = true,
            ParametersJson = JsonSerializer.Serialize(options, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
