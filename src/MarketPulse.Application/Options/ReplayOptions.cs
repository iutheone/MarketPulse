namespace MarketPulse.Application.Options;

public sealed class ReplayOptions
{
    public const string SectionName = "Replay";

    /// <summary>Directory that CSV files must live under. Relative paths are rooted at the content root.</summary>
    public string RootDirectory { get; set; } = "samples";

    public string CsvFile { get; set; } = "demo-session.csv";
    public int DelayMilliseconds { get; set; } = 0;
    public int MaxBars { get; set; } = 10_000;
}
