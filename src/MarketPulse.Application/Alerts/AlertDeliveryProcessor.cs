using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Observability;
using MarketPulse.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketPulse.Application.Alerts;

public sealed class AlertDeliveryProcessor : IAlertDeliveryProcessor
{
    private readonly IWebhookClient _webhook;
    private readonly IAlertDeliveryStore _store;
    private readonly IAlertOutboxPublisher _outbox;
    private readonly AlertOptions _options;
    private readonly ILogger<AlertDeliveryProcessor> _logger;

    public AlertDeliveryProcessor(
        IWebhookClient webhook,
        IAlertDeliveryStore store,
        IAlertOutboxPublisher outbox,
        IOptions<AlertOptions> options,
        ILogger<AlertDeliveryProcessor> logger)
    {
        _webhook = webhook;
        _store = store;
        _outbox = outbox;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProcessAsync(AlertDispatch dispatch, CancellationToken cancellationToken)
    {
        var result = await _webhook.PostAsync(dispatch, cancellationToken);
        if (result.Succeeded)
        {
            await _store.MarkAsync(dispatch.DeliveryId, "delivered", null, dispatch.Attempt, cancellationToken);
            MarketPulseTelemetry.AlertsDelivered.Add(1);
            return;
        }

        var error = result.Error ?? "Webhook failed.";
        if (dispatch.Attempt >= Math.Max(1, _options.MaxAttempts))
        {
            await _store.MarkAsync(dispatch.DeliveryId, "failed", error, dispatch.Attempt, cancellationToken);
            await _outbox.PublishDeadLetterAsync(dispatch, error, cancellationToken);
            MarketPulseTelemetry.AlertsFailed.Add(1);
            _logger.LogWarning(
                "Alert {DeliveryId} moved to DLQ after {Attempt} attempts: {Error}",
                dispatch.DeliveryId,
                dispatch.Attempt,
                error);
            return;
        }

        await _store.MarkAsync(dispatch.DeliveryId, "pending", error, dispatch.Attempt, cancellationToken);
        var delay = TimeSpan.FromMilliseconds(Math.Max(0, _options.RetryDelayMilliseconds));
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken);
        }

        await _outbox.PublishAsync(dispatch with { Attempt = dispatch.Attempt + 1 }, cancellationToken);
    }
}
