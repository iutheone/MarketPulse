namespace MarketPulse.Application.Options;

public sealed class MarketDataOptions
{
    public const string SectionName = "MarketData";

    /// <summary>Synthetic | TwelveData. Phase 1 only implements Synthetic.</summary>
    public string Provider { get; set; } = "Synthetic";

    /// <summary>Polling | Streaming</summary>
    public string Mode { get; set; } = "Streaming";

    /// <summary>Used only when Mode is Polling. Do not assume 60 seconds.</summary>
    public int PollingIntervalSeconds { get; set; } = 5;

    public string[] Symbols { get; set; } = [];
}
