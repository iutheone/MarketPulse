using System.Net.Http.Json;
using System.Text.Json;
using MarketPulse.Application.Alerts;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Mapping;
using MarketPulse.Application.Messaging;
using MarketPulse.Application.Options;
using Microsoft.Extensions.Options;

namespace MarketPulse.Infrastructure.Alerts;

public sealed class HttpWebhookClient : IWebhookClient
{
    public const string HttpClientName = "alerts-webhook";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IHttpClientFactory _http;
    private readonly AlertOptions _options;

    public HttpWebhookClient(IHttpClientFactory http, IOptions<AlertOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<WebhookSendResult> PostAsync(AlertDispatch dispatch, CancellationToken cancellationToken)
    {
        if (!AlertDestination.TryValidate(dispatch.Destination, _options.AllowHttpLoopback, out var uri, out var error))
        {
            return WebhookSendResult.Fail(error);
        }

        try
        {
            var client = _http.CreateClient(HttpClientName);
            var payload = new
            {
                eventType = EventTypes.AnomalyDetected,
                note = AlertNotes.Disclaimer,
                deliveryId = dispatch.DeliveryId,
                attempt = dispatch.Attempt,
                anomaly = ApiDtoMapper.ToDto(dispatch.Anomaly)
            };
            using var response = await client.PostAsJsonAsync(uri, payload, Json, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return WebhookSendResult.Ok();
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (body.Length > 300)
            {
                body = body[..300];
            }

            return WebhookSendResult.Fail($"{(int)response.StatusCode} {body}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return WebhookSendResult.Fail(ex.Message);
        }
    }
}
