namespace MarketPulse.Application.Contracts;

public sealed class PagedResponse<T>
{
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int Total { get; init; }
    public required IReadOnlyList<T> Items { get; init; }
}

public sealed class AnomalyDto
{
    public required Guid Id { get; init; }
    public required Guid SourceEventId { get; init; }
    public required string Symbol { get; init; }
    public required DateTimeOffset DetectedAt { get; init; }
    public required decimal Score { get; init; }
    public required string Severity { get; init; }
    public required IReadOnlyList<string> Reasons { get; init; }
    public required string RuleVersion { get; init; }
    public decimal? RelativeVolume { get; init; }
    public decimal? VolumeAcceleration { get; init; }
    public required decimal PriceChangePercent { get; init; }
    public required decimal VwapDeviationPercent { get; init; }
    public required bool Breakout { get; init; }
    public required decimal LastPrice { get; init; }
    public required long Volume1m { get; init; }
}

public sealed class AnomalyRealtimeDto
{
    public required Guid Id { get; init; }
    public required string Symbol { get; init; }
    public required decimal Score { get; init; }
    public required string Severity { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required IReadOnlyList<string> Reasons { get; init; }
}

public sealed class FeatureDto
{
    public required string Symbol { get; init; }
    public required Guid SourceEventId { get; init; }
    public required DateTimeOffset CalculatedAt { get; init; }
    public required decimal LastPrice { get; init; }
    public required long LastVolume { get; init; }
    public required long Volume1m { get; init; }
    public required long Volume5m { get; init; }
    public required long Volume15m { get; init; }
    public required decimal BaselineVolumePerMinute { get; init; }
    public decimal? RelativeVolume { get; init; }
    public decimal? VolumeAcceleration { get; init; }
    public required decimal PriceChange { get; init; }
    public required decimal PriceChangePercent { get; init; }
    public required decimal Vwap { get; init; }
    public required decimal VwapDeviationPercent { get; init; }
    public required bool BreakoutHigh { get; init; }
    public required bool BreakoutLow { get; init; }
    public required decimal Volatility { get; init; }
    public required int SampleCount { get; init; }
    public required bool HasSufficientHistory { get; init; }
}

public sealed class BarDto
{
    public required Guid EventId { get; init; }
    public required string Symbol { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required decimal Open { get; init; }
    public required decimal High { get; init; }
    public required decimal Low { get; init; }
    public required decimal Close { get; init; }
    public required long Volume { get; init; }
    public required string Source { get; init; }
}

public sealed class StockSnapshotDto
{
    public required string Symbol { get; init; }
    public BarDto? LatestBar { get; init; }
    public FeatureDto? Features { get; init; }
    public AnomalyDto? LatestAnomaly { get; init; }
    public required string Note { get; init; }
}

public sealed class WatchlistDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<string> Symbols { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreateWatchlistRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> Symbols { get; set; } = [];
}

public sealed class DetectionRuleDto
{
    public required Guid Id { get; init; }
    public required string Version { get; init; }
    public required bool IsActive { get; init; }
    public required string ParametersJson { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class UpsertDetectionRuleRequest
{
    public string Version { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string ParametersJson { get; set; } = "{}";
}

public sealed class ReplayRequest
{
    public string Source { get; set; } = "csv";
    public string? CsvFile { get; set; }
    public List<string> Symbols { get; set; } = [];
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int DelayMilliseconds { get; set; }
}

public sealed class ReplayResultDto
{
    public required int BarsRead { get; init; }
    public required int Published { get; init; }
    public required string Source { get; init; }
    public required string Note { get; init; }
}

public sealed class BacktestRequest
{
    public string Source { get; set; } = "csv";
    public string? CsvFile { get; set; }
    public List<string> Symbols { get; set; } = [];
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}

public sealed class BacktestRunDto
{
    public required Guid Id { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public required string Status { get; init; }
    public required string ParametersJson { get; init; }
    public required IReadOnlyList<BacktestResultDto> Results { get; init; }
    public string? Error { get; init; }
}

public sealed class BacktestResultDto
{
    public required Guid Id { get; init; }
    public required string Symbol { get; init; }
    public required int Bars { get; init; }
    public required int Anomalies { get; init; }
    public required decimal RecordRate { get; init; }
    public decimal? MeanScore { get; init; }
    public required string Notes { get; init; }
}
