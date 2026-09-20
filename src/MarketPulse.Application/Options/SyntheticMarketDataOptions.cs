namespace MarketPulse.Application.Options;

public sealed class SyntheticMarketDataOptions
{
    public const string SectionName = "SyntheticMarketData";

    public string[] Symbols { get; set; } = ["AAPL", "MSFT", "NVDA", "TSLA", "AMZN"];
    public int IntervalMilliseconds { get; set; } = 1000;
    public int? Seed { get; set; } = 42;
    public double VolumeSpikeProbability { get; set; } = 0.04;
    public double PriceSpikeProbability { get; set; } = 0.03;
}
