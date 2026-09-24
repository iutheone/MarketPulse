using MarketPulse.Application.Contracts;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Queries;
using MarketPulse.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketPulse.Infrastructure.Persistence;

public sealed class PostgresCatalogStore : IBarHistoryStore, IWatchlistStore, IDetectionRuleStore
{
    private readonly MarketPulseDbContext _db;

    public PostgresCatalogStore(MarketPulseDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<BarDto> Items, int Total)> SearchAsync(HistoryQuery query, CancellationToken cancellationToken)
    {
        var rows = _db.MarketBars.AsNoTracking().Where(b => b.Symbol == query.Symbol);
        if (query.From is not null)
        {
            rows = rows.Where(b => b.Timestamp >= query.From.Value);
        }

        if (query.To is not null)
        {
            rows = rows.Where(b => b.Timestamp <= query.To.Value);
        }

        var total = await rows.CountAsync(cancellationToken);
        var items = await rows
            .OrderByDescending(b => b.Timestamp)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(b => new BarDto
            {
                EventId = b.EventId,
                Symbol = b.Symbol,
                Timestamp = b.Timestamp,
                Open = b.Open,
                High = b.High,
                Low = b.Low,
                Close = b.Close,
                Volume = b.Volume,
                Source = b.Source
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    async Task<IReadOnlyList<WatchlistDto>> IWatchlistStore.ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.Watchlists.AsNoTracking()
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
        return rows.Select(ToWatchlist).ToList();
    }

    public async Task<WatchlistDto> CreateAsync(string name, IReadOnlyList<string> symbols, CancellationToken cancellationToken)
    {
        var row = new WatchlistRecord
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            SymbolsCsv = string.Join(',', symbols.Select(s => s.Trim().ToUpperInvariant()).Where(s => s.Length > 0).Distinct()),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Watchlists.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return ToWatchlist(row);
    }

    async Task<IReadOnlyList<DetectionRuleDto>> IDetectionRuleStore.ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.DetectionRules.AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(ToRule).ToList();
    }

    public async Task<DetectionRuleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await _db.DetectionRules.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return row is null ? null : ToRule(row);
    }

    public async Task<DetectionRuleDto> CreateAsync(UpsertDetectionRuleRequest request, CancellationToken cancellationToken)
    {
        var row = new DetectionRuleRecord
        {
            Id = Guid.NewGuid(),
            Version = request.Version.Trim(),
            IsActive = request.IsActive,
            ParametersJson = string.IsNullOrWhiteSpace(request.ParametersJson) ? "{}" : request.ParametersJson,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.DetectionRules.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return ToRule(row);
    }

    public async Task<DetectionRuleDto?> UpdateAsync(Guid id, UpsertDetectionRuleRequest request, CancellationToken cancellationToken)
    {
        var row = await _db.DetectionRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (row is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Version))
        {
            row.Version = request.Version.Trim();
        }

        row.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.ParametersJson))
        {
            row.ParametersJson = request.ParametersJson;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ToRule(row);
    }

    private static WatchlistDto ToWatchlist(WatchlistRecord row) => new()
    {
        Id = row.Id,
        Name = row.Name,
        Symbols = string.IsNullOrWhiteSpace(row.SymbolsCsv)
            ? []
            : row.SymbolsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        CreatedAt = row.CreatedAt
    };

    private static DetectionRuleDto ToRule(DetectionRuleRecord row) => new()
    {
        Id = row.Id,
        Version = row.Version,
        IsActive = row.IsActive,
        ParametersJson = row.ParametersJson,
        CreatedAt = row.CreatedAt
    };
}
