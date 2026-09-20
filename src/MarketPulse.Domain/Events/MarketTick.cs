namespace MarketPulse.Domain.Events;

/// <summary>
/// Canonical market event published to <c>market.normalized</c>.
/// Downstream processors must depend on this type, never on a vendor DTO.
/// </summary>
public sealed record MarketTick
{
    public required Guid EventId { get; init; }
    public required string Symbol { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required decimal Open { get; init; }
    public required decimal High { get; init; }
    public required decimal Low { get; init; }
    public required decimal Close { get; init; }
    public required long Volume { get; init; }
    public required string Source { get; init; }
    public int SchemaVersion { get; init; } = SchemaVersions.MarketTick;
}

public static class SchemaVersions
{
    public const int MarketTick = 1;
}
