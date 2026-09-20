using System.Globalization;
using MarketPulse.Domain.Entities;

namespace MarketPulse.Infrastructure.MarketData.TwelveData;

internal static class TwelveDataMapper
{
    public static MarketBar ToBar(string requestedSymbol, TwelveDataTimeSeriesResponse response)
    {
        if (response.Values is null || response.Values.Count == 0)
        {
            throw new InvalidOperationException($"Twelve Data returned no bars for {requestedSymbol}.");
        }

        var latest = response.Values[0];
        var symbol = string.IsNullOrWhiteSpace(response.Meta?.Symbol)
            ? requestedSymbol
            : response.Meta!.Symbol!;

        return new MarketBar
        {
            Symbol = symbol.ToUpperInvariant(),
            Timestamp = ParseTimestamp(latest.DateTime),
            Open = ParseDecimal(latest.Open, "open"),
            High = ParseDecimal(latest.High, "high"),
            Low = ParseDecimal(latest.Low, "low"),
            Close = ParseDecimal(latest.Close, "close"),
            Volume = ParseVolume(latest.Volume),
            Source = "twelvedata"
        };
    }

    private static DateTimeOffset ParseTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Twelve Data bar is missing datetime.");
        }

        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var timestamp))
        {
            return timestamp;
        }

        throw new InvalidOperationException($"Twelve Data datetime '{value}' could not be parsed.");
    }

    private static decimal ParseDecimal(string? value, string field)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            throw new InvalidOperationException($"Twelve Data field '{field}' is missing or invalid.");
        }

        return number;
    }

    private static long ParseVolume(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var volume))
        {
            throw new InvalidOperationException("Twelve Data field 'volume' is invalid.");
        }

        return volume;
    }
}
