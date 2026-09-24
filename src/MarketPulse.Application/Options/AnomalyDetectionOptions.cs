namespace MarketPulse.Application.Options;

public sealed class AnomalyDetectionOptions
{
    public const string SectionName = "AnomalyDetection";

    public string RuleVersion { get; set; } = "v1";
    public decimal MinScoreToRecord { get; set; } = 40m;
    public decimal RvolFullScale { get; set; } = 5m;
    public decimal AccelerationFullScale { get; set; } = 2m;
    public decimal AbsPriceChangeFullScale { get; set; } = 5m;
    public decimal VwapDeviationFullScale { get; set; } = 3m;
    public decimal WeightRvol { get; set; } = 0.35m;
    public decimal WeightAcceleration { get; set; } = 0.20m;
    public decimal WeightAbsPriceChange { get; set; } = 0.20m;
    public decimal WeightVwapDeviation { get; set; } = 0.15m;
    public decimal WeightBreakout { get; set; } = 0.10m;
}
