using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using MarketPulse.Application.Options;
using MarketPulse.Domain.Entities;
using MarketPulse.Domain.Events;
using MarketPulse.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketPulse.Infrastructure.MarketData.TwelveData;

/// <summary>
/// Polls Twelve Data <c>/time_series</c> and maps vendor JSON to canonical <see cref="MarketBar"/>.
/// Downstream Kafka publishing never sees Twelve Data types.
/// </summary>
public sealed class TwelveDataRestProvider : IMarketDataProvider
{
    public const string HttpClientName = "TwelveData";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TwelveDataOptions _options;
    private readonly ILogger<TwelveDataRestProvider> _logger;

    public TwelveDataRestProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<TwelveDataOptions> options,
        ILogger<TwelveDataRestProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MarketBar>> GetLatestAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken)
    {
        EnsureApiKey();

        var list = symbols.Select(s => s.ToUpperInvariant()).Distinct().ToArray();
        var bars = new List<MarketBar>(list.Length);

        for (var i = 0; i < list.Length; i++)
        {
            if (i > 0 && _options.DelayBetweenSymbolsMilliseconds > 0)
            {
                await Task.Delay(_options.DelayBetweenSymbolsMilliseconds, cancellationToken);
            }

            var bar = await FetchLatestBarAsync(list[i], allowRateLimitRetry: true, cancellationToken);
            if (bar is not null)
            {
                bars.Add(bar);
            }
        }

        return bars;
    }

    public IAsyncEnumerable<MarketTick> StreamAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "Twelve Data REST does not stream. Set MarketData:Mode to Polling until TwelveDataWebSocketProvider exists.");
    }

    private async Task<MarketBar?> FetchLatestBarAsync(
        string symbol,
        bool allowRateLimitRetry,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"time_series?symbol={Uri.EscapeDataString(symbol)}&interval={Uri.EscapeDataString(_options.Interval)}&outputsize={_options.OutputSize}&timezone=UTC&order=desc");
        request.Headers.Authorization = new AuthenticationHeaderValue("apikey", _options.ApiKey);

        _logger.LogInformation(
            "Requesting Twelve Data time_series for {Symbol} interval {Interval}",
            symbol,
            _options.Interval);

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        TwelveDataTimeSeriesResponse? payload;
        try
        {
            payload = JsonSerializer.Deserialize<TwelveDataTimeSeriesResponse>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Twelve Data returned non-JSON for {Symbol} (HTTP {StatusCode})", symbol, (int)response.StatusCode);
            return null;
        }

        if (payload is null)
        {
            _logger.LogWarning("Twelve Data returned an empty payload for {Symbol}", symbol);
            return null;
        }

        if (IsRateLimited(response.StatusCode, payload))
        {
            if (!allowRateLimitRetry)
            {
                _logger.LogWarning("Twelve Data rate-limited {Symbol} after retry. Skipping this poll.", symbol);
                return null;
            }

            var wait = TimeSpan.FromSeconds(Math.Max(0, _options.RateLimitRetrySeconds));
            _logger.LogWarning(
                "Twelve Data rate-limited {Symbol}. Waiting {DelaySeconds}s then retrying once.",
                symbol,
                wait.TotalSeconds);
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, cancellationToken);
            }
            return await FetchLatestBarAsync(symbol, allowRateLimitRetry: false, cancellationToken);
        }

        if (!response.IsSuccessStatusCode || IsApiError(payload))
        {
            _logger.LogWarning(
                "Twelve Data rejected {Symbol}: HTTP {StatusCode} code={ErrorCode} message={Message}",
                symbol,
                (int)response.StatusCode,
                payload.Code,
                payload.Message);
            return null;
        }

        try
        {
            var bar = TwelveDataMapper.ToBar(symbol, payload);
            _logger.LogInformation(
                "Mapped Twelve Data bar {Symbol} at {Timestamp} close={Close} volume={Volume}",
                bar.Symbol,
                bar.Timestamp,
                bar.Close,
                bar.Volume);
            return bar;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Twelve Data payload for {Symbol} failed validation", symbol);
            return null;
        }
    }

    private void EnsureApiKey()
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return;
        }

        throw new InvalidOperationException(
            "TwelveData:ApiKey is missing. Set user secret TwelveData:ApiKey or environment variable TwelveData__ApiKey. Do not commit the key.");
    }

    private static bool IsRateLimited(HttpStatusCode status, TwelveDataTimeSeriesResponse payload) =>
        status == HttpStatusCode.TooManyRequests || payload.Code == 429;

    private static bool IsApiError(TwelveDataTimeSeriesResponse payload) =>
        payload.Code is >= 400 ||
        string.Equals(payload.Status, "error", StringComparison.OrdinalIgnoreCase);
}
