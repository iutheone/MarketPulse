namespace MarketPulse.Domain.Anomaly;

/// <summary>
/// Tunable scoring inputs. Defaults match the portfolio spec; they are not scientifically optimal.
/// </summary>
public sealed class DetectionParameters
{
    public string RuleVersion { get; init; } = "v1";
    public decimal MinScoreToRecord { get; init; } = 40m;
    public decimal RvolFullScale { get; init; } = 5m;
    public decimal AccelerationFullScale { get; init; } = 2m;
    public decimal AbsPriceChangeFullScale { get; init; } = 5m;
    public decimal VwapDeviationFullScale { get; init; } = 3m;
    public decimal WeightRvol { get; init; } = 0.35m;
    public decimal WeightAcceleration { get; init; } = 0.20m;
    public decimal WeightAbsPriceChange { get; init; } = 0.20m;
    public decimal WeightVwapDeviation { get; init; } = 0.15m;
    public decimal WeightBreakout { get; init; } = 0.10m;
}
