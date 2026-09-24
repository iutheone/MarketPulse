using MarketPulse.Domain.Enums;

namespace MarketPulse.Application.Queries;

public sealed class AnomalyQuery
{
    public string? Symbol { get; init; }
    public string? Severity { get; init; }
    public string Sort { get; init; } = "detectedAt";
    public string Direction { get; init; } = "desc";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public static AnomalyQuery Normalize(
        string? symbol,
        string? severity,
        string? sort,
        string? direction,
        int page,
        int pageSize)
    {
        var sortKey = (sort ?? "detectedAt").Trim().ToLowerInvariant();
        if (sortKey is not ("detectedat" or "score" or "symbol" or "severity"))
        {
            sortKey = "detectedat";
        }

        var dir = (direction ?? "desc").Trim().ToLowerInvariant();
        if (dir is not ("asc" or "desc"))
        {
            dir = "desc";
        }

        string? sev = null;
        if (!string.IsNullOrWhiteSpace(severity) && Enum.TryParse<AnomalySeverity>(severity.Trim(), true, out var parsed))
        {
            sev = parsed.ToString();
        }

        return new AnomalyQuery
        {
            Symbol = string.IsNullOrWhiteSpace(symbol) ? null : symbol.Trim().ToUpperInvariant(),
            Severity = sev,
            Sort = sortKey,
            Direction = dir,
            Page = page <= 0 ? 1 : page,
            PageSize = pageSize <= 0 ? 20 : Math.Clamp(pageSize, 1, 100)
        };
    }
}

public sealed class HistoryQuery
{
    public required string Symbol { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;

    public static HistoryQuery Normalize(string symbol, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize)
    {
        return new HistoryQuery
        {
            Symbol = symbol.Trim().ToUpperInvariant(),
            From = from,
            To = to,
            Page = page <= 0 ? 1 : page,
            PageSize = pageSize <= 0 ? 50 : Math.Clamp(pageSize, 1, 500)
        };
    }
}
