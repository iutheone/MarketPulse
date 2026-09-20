namespace MarketPulse.Domain.Features;

public sealed record TickSample
{
    public required Guid EventId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required decimal Open { get; init; }
    public required decimal High { get; init; }
    public required decimal Low { get; init; }
    public required decimal Close { get; init; }
    public required long Volume { get; init; }

    public decimal TypicalPrice => (High + Low + Close) / 3m;
}
