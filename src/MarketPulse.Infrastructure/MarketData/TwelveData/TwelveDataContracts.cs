using System.Text.Json.Serialization;

namespace MarketPulse.Infrastructure.MarketData.TwelveData;

internal sealed class TwelveDataTimeSeriesResponse
{
    [JsonPropertyName("meta")]
    public TwelveDataMeta? Meta { get; set; }

    [JsonPropertyName("values")]
    public List<TwelveDataBarDto>? Values { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("code")]
    public int? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

internal sealed class TwelveDataMeta
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("interval")]
    public string? Interval { get; set; }

    [JsonPropertyName("exchange_timezone")]
    public string? ExchangeTimezone { get; set; }
}

internal sealed class TwelveDataBarDto
{
    [JsonPropertyName("datetime")]
    public string? DateTime { get; set; }

    [JsonPropertyName("open")]
    public string? Open { get; set; }

    [JsonPropertyName("high")]
    public string? High { get; set; }

    [JsonPropertyName("low")]
    public string? Low { get; set; }

    [JsonPropertyName("close")]
    public string? Close { get; set; }

    [JsonPropertyName("volume")]
    public string? Volume { get; set; }
}
