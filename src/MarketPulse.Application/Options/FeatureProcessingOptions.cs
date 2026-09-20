namespace MarketPulse.Application.Options;

public sealed class FeatureProcessingOptions
{
    public const string SectionName = "FeatureProcessing";

    /// <summary>How long tick samples stay in the Redis rolling window.</summary>
    public int RetentionMinutes { get; set; } = 60;
}
