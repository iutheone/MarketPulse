using MarketPulse.Domain.Anomaly;

namespace MarketPulse.Application.Alerts;

public sealed record AlertDispatch
{
    public required Guid DeliveryId { get; init; }
    public required Guid ConfigurationId { get; init; }
    public required string Destination { get; init; }
    public required int Attempt { get; init; }
    public required AnomalyResult Anomaly { get; init; }
}

public sealed record AlertDeadLetter
{
    public required AlertDispatch Dispatch { get; init; }
    public required string Error { get; init; }
}

public sealed class WebhookSendResult
{
    public required bool Succeeded { get; init; }
    public string? Error { get; init; }

    public static WebhookSendResult Ok() => new() { Succeeded = true };

    public static WebhookSendResult Fail(string error) => new() { Succeeded = false, Error = error };
}
