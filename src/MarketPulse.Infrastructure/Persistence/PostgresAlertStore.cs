using MarketPulse.Application.Alerts;
using MarketPulse.Application.Contracts;
using MarketPulse.Application.Interfaces;
using MarketPulse.Domain.Alerts;
using MarketPulse.Domain.Anomaly;
using MarketPulse.Domain.Enums;
using MarketPulse.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MarketPulse.Infrastructure.Persistence;

public sealed class PostgresAlertStore : IAlertConfigurationStore, IAlertDeliveryStore
{
    private readonly MarketPulseDbContext _db;

    public PostgresAlertStore(MarketPulseDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AlertConfigurationDto>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.AlertConfigurations.AsNoTracking()
            .OrderBy(c => c.Symbol)
            .ToListAsync(cancellationToken);
        return rows.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<AlertRule>> ListEnabledAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.AlertConfigurations.AsNoTracking()
            .Where(c => c.IsEnabled)
            .ToListAsync(cancellationToken);
        return rows.Select(ToRule).OfType<AlertRule>().ToList();
    }

    public async Task<AlertConfigurationDto> CreateAsync(UpsertAlertConfigurationRequest request, CancellationToken cancellationToken)
    {
        var row = ToRecord(Guid.NewGuid(), request);
        _db.AlertConfigurations.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(row);
    }

    public async Task<AlertConfigurationDto?> UpdateAsync(Guid id, UpsertAlertConfigurationRequest request, CancellationToken cancellationToken)
    {
        var row = await _db.AlertConfigurations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (row is null)
        {
            return null;
        }

        row.Symbol = NormalizeSymbol(request.Symbol);
        row.MinSeverity = request.MinSeverity.Trim();
        row.Channel = string.IsNullOrWhiteSpace(request.Channel) ? AlertMatcher.WebhookChannel : request.Channel.Trim().ToLowerInvariant();
        row.Destination = request.Destination.Trim();
        row.IsEnabled = request.IsEnabled;
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(row);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await _db.AlertConfigurations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (row is null)
        {
            return false;
        }

        _db.AlertConfigurations.Remove(row);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<AlertDispatch?> TryEnqueueAsync(AnomalyResult anomaly, AlertRule rule, CancellationToken cancellationToken)
    {
        var row = new AlertDeliveryRecord
        {
            Id = Guid.NewGuid(),
            AnomalyId = anomaly.AnomalyId,
            ConfigurationId = rule.Id,
            Status = "pending",
            AttemptCount = 0,
            AttemptedAt = DateTimeOffset.UtcNow
        };
        _db.AlertDeliveries.Add(row);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _db.Entry(row).State = EntityState.Detached;
            return null;
        }

        return new AlertDispatch
        {
            DeliveryId = row.Id,
            ConfigurationId = rule.Id,
            Destination = rule.Destination,
            Attempt = 1,
            Anomaly = anomaly
        };
    }

    public async Task MarkAsync(Guid deliveryId, string status, string? error, int attemptCount, CancellationToken cancellationToken)
    {
        var row = await _db.AlertDeliveries.FirstOrDefaultAsync(d => d.Id == deliveryId, cancellationToken);
        if (row is null)
        {
            return;
        }

        row.Status = status;
        row.Error = error;
        row.AttemptCount = attemptCount;
        row.AttemptedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlertDeliveryDto>> ListAsync(Guid? anomalyId, string? status, CancellationToken cancellationToken)
    {
        var query = _db.AlertDeliveries.AsNoTracking().AsQueryable();
        if (anomalyId is not null)
        {
            query = query.Where(d => d.AnomalyId == anomalyId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(d => d.Status == status);
        }

        var rows = await query
            .OrderByDescending(d => d.AttemptedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
        return rows.Select(d => new AlertDeliveryDto
        {
            Id = d.Id,
            AnomalyId = d.AnomalyId,
            ConfigurationId = d.ConfigurationId,
            Status = d.Status,
            AttemptCount = d.AttemptCount,
            AttemptedAt = d.AttemptedAt,
            Error = d.Error
        }).ToList();
    }

    private static AlertConfigurationRecord ToRecord(Guid id, UpsertAlertConfigurationRequest request) => new()
    {
        Id = id,
        Symbol = NormalizeSymbol(request.Symbol),
        MinSeverity = request.MinSeverity.Trim(),
        Channel = string.IsNullOrWhiteSpace(request.Channel) ? AlertMatcher.WebhookChannel : request.Channel.Trim().ToLowerInvariant(),
        Destination = request.Destination.Trim(),
        IsEnabled = request.IsEnabled
    };

    private static AlertConfigurationDto ToDto(AlertConfigurationRecord row) => new()
    {
        Id = row.Id,
        Symbol = row.Symbol,
        MinSeverity = row.MinSeverity,
        Channel = row.Channel,
        Destination = row.Destination,
        IsEnabled = row.IsEnabled
    };

    private static AlertRule? ToRule(AlertConfigurationRecord row)
    {
        if (!Enum.TryParse<AnomalySeverity>(row.MinSeverity, ignoreCase: true, out var min))
        {
            return null;
        }

        return new AlertRule
        {
            Id = row.Id,
            Symbol = row.Symbol,
            MinSeverity = min,
            Channel = row.Channel,
            Destination = row.Destination,
            IsEnabled = row.IsEnabled
        };
    }

    private static string NormalizeSymbol(string symbol)
    {
        var trimmed = symbol.Trim();
        return trimmed == "*" || trimmed.Length == 0 ? "*" : trimmed.ToUpperInvariant();
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation;
}
