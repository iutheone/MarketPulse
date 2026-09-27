using MarketPulse.Application.Alerts;
using MarketPulse.Application.Contracts;
using MarketPulse.Domain.Alerts;
using MarketPulse.Domain.Anomaly;

namespace MarketPulse.Application.Interfaces;

public interface IAlertConfigurationStore
{
    Task<IReadOnlyList<AlertConfigurationDto>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertRule>> ListEnabledAsync(CancellationToken cancellationToken);
    Task<AlertConfigurationDto> CreateAsync(UpsertAlertConfigurationRequest request, CancellationToken cancellationToken);
    Task<AlertConfigurationDto?> UpdateAsync(Guid id, UpsertAlertConfigurationRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public interface IAlertDeliveryStore
{
    Task<AlertDispatch?> TryEnqueueAsync(AnomalyResult anomaly, AlertRule rule, CancellationToken cancellationToken);
    Task MarkAsync(Guid deliveryId, string status, string? error, int attemptCount, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertDeliveryDto>> ListAsync(Guid? anomalyId, string? status, CancellationToken cancellationToken);
}

public interface IAlertOutboxPublisher
{
    Task PublishAsync(AlertDispatch dispatch, CancellationToken cancellationToken);
    Task PublishDeadLetterAsync(AlertDispatch dispatch, string error, CancellationToken cancellationToken);
}

public interface IWebhookClient
{
    Task<WebhookSendResult> PostAsync(AlertDispatch dispatch, CancellationToken cancellationToken);
}

public interface IAlertDispatcher
{
    Task DispatchAsync(AnomalyResult anomaly, CancellationToken cancellationToken);
}

public interface IAlertDeliveryProcessor
{
    Task ProcessAsync(AlertDispatch dispatch, CancellationToken cancellationToken);
}

public interface IAlertDispatchConsumer
{
    Task StartAsync(CancellationToken cancellationToken);
}

public interface IAlertDeliveryConsumer
{
    Task StartAsync(CancellationToken cancellationToken);
}
