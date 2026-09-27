using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Observability;
using MarketPulse.Domain.Alerts;
using MarketPulse.Domain.Anomaly;
using Microsoft.Extensions.Logging;

namespace MarketPulse.Application.Alerts;

public sealed class AlertDispatcher : IAlertDispatcher
{
    private readonly IAlertConfigurationStore _configs;
    private readonly IAlertDeliveryStore _deliveries;
    private readonly IAlertOutboxPublisher _outbox;
    private readonly ILogger<AlertDispatcher> _logger;

    public AlertDispatcher(
        IAlertConfigurationStore configs,
        IAlertDeliveryStore deliveries,
        IAlertOutboxPublisher outbox,
        ILogger<AlertDispatcher> logger)
    {
        _configs = configs;
        _deliveries = deliveries;
        _outbox = outbox;
        _logger = logger;
    }

    public async Task DispatchAsync(AnomalyResult anomaly, CancellationToken cancellationToken)
    {
        var rules = await _configs.ListEnabledAsync(cancellationToken);
        foreach (var rule in rules)
        {
            if (!AlertMatcher.Matches(rule, anomaly))
            {
                continue;
            }

            var dispatch = await _deliveries.TryEnqueueAsync(anomaly, rule, cancellationToken);
            if (dispatch is null)
            {
                _logger.LogInformation(
                    "Skipped duplicate alert for anomaly {AnomalyId} config {ConfigId}",
                    anomaly.AnomalyId,
                    rule.Id);
                continue;
            }

            await _outbox.PublishAsync(dispatch, cancellationToken);
            MarketPulseTelemetry.AlertsDispatched.Add(1);
        }
    }
}
