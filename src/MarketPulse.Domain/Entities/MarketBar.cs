namespace MarketPulse.Domain.Entities;

/// <summary>
/// OHLCV bar in canonical form. Provider-specific payloads must be mapped here
/// before they leave the infrastructure market-data adapters.
/// </summary>
public sealed record MarketBar
{
    public required string Symbol { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required decimal Open { get; init; }
    public required decimal High { get; init; }
    public required decimal Low { get; init; }
    public required decimal Close { get; init; }
    public required long Volume { get; init; }
    public required string Source { get; init; }
}
