using MarketPulse.Domain.Enums;

namespace MarketPulse.Infrastructure.Persistence.Entities;

public sealed class ProcessedEventRecord
{
    public Guid EventId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; }
}

public sealed class MarketEventRecord
{
    public Guid EventId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string Source { get; set; } = string.Empty;
    public int SchemaVersion { get; set; }
    public string PayloadJson { get; set; } = "{}";
}

public sealed class MarketBarRecord
{
    public Guid EventId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
    public string Source { get; set; } = string.Empty;
}

public sealed class AnomalyRecord
{
    public Guid Id { get; set; }
    public Guid SourceEventId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateTimeOffset DetectedAt { get; set; }
    public decimal Score { get; set; }
    public string Severity { get; set; } = nameof(AnomalySeverity.Elevated);
    public string ReasonsJson { get; set; } = "[]";
    public string RuleVersion { get; set; } = "v1";
    public decimal? RelativeVolume { get; set; }
    public decimal? VolumeAcceleration { get; set; }
    public decimal PriceChangePercent { get; set; }
    public decimal VwapDeviationPercent { get; set; }
    public bool Breakout { get; set; }
    public decimal LastPrice { get; set; }
    public long Volume1m { get; set; }
}

public sealed class DetectionRuleRecord
{
    public Guid Id { get; set; }
    public string Version { get; set; } = "v1";
    public bool IsActive { get; set; }
    public string ParametersJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AlertConfigurationRecord
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string MinSeverity { get; set; } = nameof(AnomalySeverity.High);
    public string Channel { get; set; } = "webhook";
    public string Destination { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

public sealed class AlertDeliveryRecord
{
    public Guid Id { get; set; }
    public Guid AnomalyId { get; set; }
    public Guid? ConfigurationId { get; set; }
    public string Status { get; set; } = "pending";
    public DateTimeOffset AttemptedAt { get; set; }
    public string? Error { get; set; }
}

public sealed class BacktestRunRecord
{
    public Guid Id { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public string Status { get; set; } = "queued";
    public string ParametersJson { get; set; } = "{}";
}

public sealed class BacktestResultRecord
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal? HitRate { get; set; }
    public string? Notes { get; set; }
}

public sealed class WatchlistRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SymbolsCsv { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
