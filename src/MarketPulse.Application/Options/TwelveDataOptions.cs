namespace MarketPulse.Application.Options;

public sealed class TwelveDataOptions
{
    public const string SectionName = "TwelveData";

    public string BaseUrl { get; set; } = "https://api.twelvedata.com/";
    public string ApiKey { get; set; } = string.Empty;
    public string Interval { get; set; } = "1min";
    public int TimeoutSeconds { get; set; } = 15;
    public int OutputSize { get; set; } = 1;
    public int DelayBetweenSymbolsMilliseconds { get; set; } = 1000;
    public int RateLimitRetrySeconds { get; set; } = 60;
}
