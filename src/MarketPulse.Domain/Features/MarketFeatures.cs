namespace MarketPulse.Domain.Features;

/// <summary>
/// Hot-path market features. Values are engineering metrics, not a forecast or a trading signal.
/// </summary>
public sealed record MarketFeatures
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
