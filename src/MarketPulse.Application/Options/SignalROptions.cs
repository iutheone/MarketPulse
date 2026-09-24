namespace MarketPulse.Application.Options;

public sealed class SignalROptions
{
    public const string SectionName = "SignalR";

    public string[] AllowedOrigins { get; set; } = ["http://localhost:5173"];
    public bool UseRedisBackplane { get; set; } = true;
}
