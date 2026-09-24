using MarketPulse.Domain.Entities;
using System.Globalization;

namespace MarketPulse.Application.Replay;

/// <summary>
/// Canonical CSV: symbol,timestamp,open,high,low,close,volume
/// Timestamp is ISO-8601. Source is always "csv" so EventId stays deterministic for a given row.
/// </summary>
public static class CsvMarketBarParser
{
    public static IReadOnlyList<MarketBar> Parse(string csv, string source = "csv")
    {
        var bars = new List<MarketBar>();
        using var reader = new StringReader(csv);
        var header = reader.ReadLine();
        if (header is null)
        {
            return bars;
        }

        string? line;
        var lineNumber = 1;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length < 7)
            {
                throw new FormatException($"CSV line {lineNumber} needs 7 columns.");
            }

            bars.Add(new MarketBar
            {
                Symbol = parts[0].Trim().ToUpperInvariant(),
                Timestamp = DateTimeOffset.Parse(parts[1].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                Open = decimal.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                High = decimal.Parse(parts[3].Trim(), CultureInfo.InvariantCulture),
                Low = decimal.Parse(parts[4].Trim(), CultureInfo.InvariantCulture),
                Close = decimal.Parse(parts[5].Trim(), CultureInfo.InvariantCulture),
                Volume = long.Parse(parts[6].Trim(), CultureInfo.InvariantCulture),
                Source = source
            });
        }

        return bars.OrderBy(b => b.Timestamp).ThenBy(b => b.Symbol).ToList();
    }
}
