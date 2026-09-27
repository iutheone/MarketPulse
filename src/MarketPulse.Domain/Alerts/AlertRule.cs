using MarketPulse.Domain.Enums;

namespace MarketPulse.Domain.Alerts;

public sealed record AlertRule
{
    public required Guid Id { get; init; }
    public required string Symbol { get; init; }
    public required AnomalySeverity MinSeverity { get; init; }
    public required string Channel { get; init; }
    public required string Destination { get; init; }
    public required bool IsEnabled { get; init; }
}
