using MarketPulse.Domain.Enums;

namespace MarketPulse.Domain.Anomaly;

/// <summary>
/// Explainable detection output. Score is an engineering metric of unusual activity, not a probability or a trade signal.
/// </summary>
public sealed record AnomalyResult
{
    public required Guid AnomalyId { get; init; }
    public required Guid SourceEventId { get; init; }
    public required string Symbol { get; init; }
    public required DateTimeOffset DetectedAt { get; init; }
    public required decimal Score { get; init; }
    public required AnomalySeverity Severity { get; init; }
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
