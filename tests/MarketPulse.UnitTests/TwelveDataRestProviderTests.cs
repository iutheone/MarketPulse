using System.Net;
using System.Text;
using MarketPulse.Application.Options;
using MarketPulse.Infrastructure.MarketData.TwelveData;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MarketPulse.UnitTests;

public class TwelveDataRestProviderTests
{
    [Fact]
    public async Task GetLatestAsync_MapsTimeSeriesJson_ToCanonicalBar()
    {
        const string json = """
            {
              "meta": { "symbol": "AAPL", "interval": "1min", "exchange_timezone": "America/New_York" },
              "values": [
                {
                  "datetime": "2026-09-19 15:59:00",
                  "open": "190.10",
                  "high": "190.80",
                  "low": "190.00",
                  "close": "190.55",
                  "volume": "123456"
                }
              ],
              "status": "ok"
            }
            """;

        var handler = new StubHandler(HttpStatusCode.OK, json);
        var provider = CreateProvider(handler);
        var bars = await provider.GetLatestAsync(["AAPL"], CancellationToken.None);

        var bar = Assert.Single(bars);
        Assert.Equal("AAPL", bar.Symbol);
        Assert.Equal(190.55m, bar.Close);
        Assert.Equal(123456, bar.Volume);
        Assert.Equal("twelvedata", bar.Source);
        Assert.Equal(new DateTimeOffset(2026, 9, 19, 15, 59, 0, TimeSpan.Zero), bar.Timestamp);
        Assert.Equal("apikey", handler.LastAuthScheme);
        Assert.DoesNotContain("apikey=", handler.LastUri!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetLatestAsync_RetriesOnce_OnHttp429()
    {
        var handler = new StubHandler(
            (HttpStatusCode.TooManyRequests, """{"code":429,"status":"error","message":"rate limit"}"""),
            (HttpStatusCode.OK, """
                {
                  "meta": { "symbol": "MSFT" },
                  "values": [
                    { "datetime": "2026-09-19 16:00:00", "open": "1", "high": "1", "low": "1", "close": "1", "volume": "10" }
                  ],
                  "status": "ok"
                }
                """));

        var provider = CreateProvider(handler);
        var bars = await provider.GetLatestAsync(["MSFT"], CancellationToken.None);

        Assert.Single(bars);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task GetLatestAsync_SkipsSymbol_OnApiError()
    {
        var handler = new StubHandler(
            HttpStatusCode.OK,
            """{ "code": 401, "status": "error", "message": "Invalid API key" }""");
        var provider = CreateProvider(handler);

        var bars = await provider.GetLatestAsync(["AAPL"], CancellationToken.None);

        Assert.Empty(bars);
    }

    [Fact]
    public void StreamAsync_IsNotSupported()
    {
        var provider = CreateProvider(new StubHandler(HttpStatusCode.OK, "{}"));
        Assert.Throws<NotSupportedException>(() => provider.StreamAsync(["AAPL"], CancellationToken.None));
    }

    private static TwelveDataRestProvider CreateProvider(StubHandler handler)
    {
        return new TwelveDataRestProvider(
            new StubFactory(handler),
            Options.Create(new TwelveDataOptions
            {
                ApiKey = "test-key",
                Interval = "1min",
                DelayBetweenSymbolsMilliseconds = 0,
                RateLimitRetrySeconds = 0
            }),
            NullLogger<TwelveDataRestProvider>.Instance);
    }

    private sealed class StubFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public StubFactory(HttpMessageHandler handler) => _handler = handler;

        public HttpClient CreateClient(string name) =>
            new(_handler, disposeHandler: false) { BaseAddress = new Uri("https://api.twelvedata.com/") };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> _responses = new();

        public int Calls { get; private set; }
        public string? LastUri { get; private set; }
        public string? LastAuthScheme { get; private set; }

        public StubHandler(HttpStatusCode status, string body) => _responses.Enqueue((status, body));

        public StubHandler(params (HttpStatusCode Status, string Body)[] responses)
        {
            foreach (var response in responses)
            {
                _responses.Enqueue(response);
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastUri = request.RequestUri?.ToString();
            LastAuthScheme = request.Headers.Authorization?.Scheme;
            var (status, body) = _responses.Dequeue();
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
