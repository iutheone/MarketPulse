using MarketPulse.Application.Alerts;
using MarketPulse.Application.Contracts;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Options;
using MarketPulse.Domain.Alerts;
using MarketPulse.Domain.Anomaly;
using MarketPulse.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MarketPulse.UnitTests;

public class AlertingTests
{
    [Fact]
    public void Matcher_IgnoresDisabledAndBelowThreshold()
    {
        var rule = Rule(AnomalySeverity.High, enabled: false);
        Assert.False(AlertMatcher.Matches(rule, Anomaly(AnomalySeverity.Extreme)));

        rule = Rule(AnomalySeverity.High, enabled: true);
        Assert.False(AlertMatcher.Matches(rule, Anomaly(AnomalySeverity.Elevated)));
        Assert.True(AlertMatcher.Matches(rule, Anomaly(AnomalySeverity.High)));
        Assert.True(AlertMatcher.Matches(rule, Anomaly(AnomalySeverity.Extreme)));
    }

    [Fact]
    public void Matcher_WildcardMatchesAnySymbol()
    {
        var rule = Rule(AnomalySeverity.Elevated, enabled: true) with { Symbol = "*" };
        Assert.True(AlertMatcher.Matches(rule, Anomaly(AnomalySeverity.High) with { Symbol = "NVDA" }));
    }

    [Fact]
    public async Task Dispatcher_PublishesOncePerMatchingConfig()
    {
        var configs = new FakeConfigs([Rule(AnomalySeverity.High, enabled: true)]);
        var store = new FakeDeliveries();
        var outbox = new FakeOutbox();
        var dispatcher = new AlertDispatcher(configs, store, outbox, NullLogger<AlertDispatcher>.Instance);

        await dispatcher.DispatchAsync(Anomaly(AnomalySeverity.High), CancellationToken.None);
        await dispatcher.DispatchAsync(Anomaly(AnomalySeverity.High), CancellationToken.None);

        Assert.Equal(2, store.Inserts);
        Assert.Equal(1, outbox.Outbox);
    }

    [Fact]
    public async Task Delivery_RetriesThenDeadLetters()
    {
        var webhook = new FakeWebhook { Succeed = false };
        var store = new FakeDeliveries();
        var outbox = new FakeOutbox();
        var processor = new AlertDeliveryProcessor(
            webhook,
            store,
            outbox,
            Options.Create(new AlertOptions
            {
                MaxAttempts = 2,
                RetryDelayMilliseconds = 0
            }),
            NullLogger<AlertDeliveryProcessor>.Instance);

        var first = Dispatch(attempt: 1);
        await processor.ProcessAsync(first, CancellationToken.None);
        Assert.Equal("pending", store.LastStatus);
        Assert.Equal(1, outbox.Outbox);

        await processor.ProcessAsync(Dispatch(attempt: 2), CancellationToken.None);
        Assert.Equal("failed", store.LastStatus);
        Assert.Equal(1, outbox.Dlq);
    }

    [Fact]
    public void Destination_RejectsHttpRemote()
    {
        Assert.False(AlertDestination.TryValidate("http://example.com/hook", allowHttpLoopback: true, out _, out _));
        Assert.True(AlertDestination.TryValidate("http://localhost:9/hook", allowHttpLoopback: true, out _, out _));
        Assert.True(AlertDestination.TryValidate("https://example.com/hook", allowHttpLoopback: false, out _, out _));
    }

    private static AlertRule Rule(AnomalySeverity min, bool enabled) => new()
    {
        Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        Symbol = "AAPL",
        MinSeverity = min,
        Channel = AlertMatcher.WebhookChannel,
        Destination = "https://example.com/hook",
        IsEnabled = enabled
    };

    private static AnomalyResult Anomaly(AnomalySeverity severity) => new()
    {
        AnomalyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        SourceEventId = Guid.NewGuid(),
        Symbol = "AAPL",
        DetectedAt = DateTimeOffset.UtcNow,
        Score = 80,
        Severity = severity,
        Reasons = ["rvol"],
        RuleVersion = "v1",
        PriceChangePercent = 1,
        VwapDeviationPercent = 1,
        Breakout = false,
        LastPrice = 1,
        Volume1m = 1
    };

    private static AlertDispatch Dispatch(int attempt) => new()
    {
        DeliveryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
        ConfigurationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        Destination = "https://example.com/hook",
        Attempt = attempt,
        Anomaly = Anomaly(AnomalySeverity.High)
    };

    private sealed class FakeConfigs : IAlertConfigurationStore
    {
        private readonly IReadOnlyList<AlertRule> _rules;
        public FakeConfigs(IReadOnlyList<AlertRule> rules) => _rules = rules;
        public Task<IReadOnlyList<AlertConfigurationDto>> ListAsync(CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<AlertRule>> ListEnabledAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_rules);
        public Task<AlertConfigurationDto> CreateAsync(UpsertAlertConfigurationRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<AlertConfigurationDto?> UpdateAsync(Guid id, UpsertAlertConfigurationRequest request, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class FakeDeliveries : IAlertDeliveryStore
    {
        public int Inserts { get; private set; }
        public string? LastStatus { get; private set; }

        public Task<AlertDispatch?> TryEnqueueAsync(AnomalyResult anomaly, AlertRule rule, CancellationToken cancellationToken)
        {
            Inserts++;
            if (Inserts > 1)
            {
                return Task.FromResult<AlertDispatch?>(null);
            }

            return Task.FromResult<AlertDispatch?>(new AlertDispatch
            {
                DeliveryId = Guid.NewGuid(),
                ConfigurationId = rule.Id,
                Destination = rule.Destination,
                Attempt = 1,
                Anomaly = anomaly
            });
        }

        public Task MarkAsync(Guid deliveryId, string status, string? error, int attemptCount, CancellationToken cancellationToken)
        {
            LastStatus = status;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AlertDeliveryDto>> ListAsync(Guid? anomalyId, string? status, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    private sealed class FakeOutbox : IAlertOutboxPublisher
    {
        public int Outbox { get; private set; }
        public int Dlq { get; private set; }
        public Task PublishAsync(AlertDispatch dispatch, CancellationToken cancellationToken)
        {
            Outbox++;
            return Task.CompletedTask;
        }

        public Task PublishDeadLetterAsync(AlertDispatch dispatch, string error, CancellationToken cancellationToken)
        {
            Dlq++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWebhook : IWebhookClient
    {
        public bool Succeed { get; set; }
        public Task<WebhookSendResult> PostAsync(AlertDispatch dispatch, CancellationToken cancellationToken) =>
            Task.FromResult(Succeed ? WebhookSendResult.Ok() : WebhookSendResult.Fail("500"));
    }
}
