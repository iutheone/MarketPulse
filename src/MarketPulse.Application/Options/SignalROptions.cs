namespace MarketPulse.Application.Options;

public sealed class SignalROptions
{
    public const string SectionName = "SignalR";

    public string[] AllowedOrigins { get; set; } = ["http://localhost:5173", "http://localhost:8088"];
    public bool UseRedisBackplane { get; set; } = true;
}
