namespace MarketPulse.Application.Options;

public sealed class AlertOptions
{
    public const string SectionName = "Alerts";

    public string DispatcherGroupId { get; set; } = "marketpulse-alert-dispatcher";
    public string DeliveryGroupId { get; set; } = "marketpulse-alert-delivery";
    public int MaxAttempts { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 2000;
    public int WebhookTimeoutSeconds { get; set; } = 10;
    public bool AllowHttpLoopback { get; set; } = true;
}
