using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Mapping;
using MarketPulse.Domain.Anomaly;
using Microsoft.AspNetCore.SignalR;

namespace MarketPulse.Api.Hubs;

public sealed class AnomalyHub : Hub
{
    public const string Path = "/anomalyHub";
    public const string ClientMethod = "anomalyDetected";
    public const string AllGroup = "all";

    public async Task Subscribe(string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AllGroup);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, symbol.Trim().ToUpperInvariant());
    }

    public async Task Unsubscribe(string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, AllGroup);
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, symbol.Trim().ToUpperInvariant());
    }
}

public sealed class SignalRAnomalyPublisher : IAnomalyRealtimePublisher
{
    private readonly IHubContext<AnomalyHub> _hub;

    public SignalRAnomalyPublisher(IHubContext<AnomalyHub> hub)
    {
        _hub = hub;
    }

    public async Task PublishAsync(AnomalyResult anomaly, CancellationToken cancellationToken)
    {
        var payload = ApiDtoMapper.ToRealtime(anomaly);
        await _hub.Clients.Group(AnomalyHub.AllGroup).SendAsync(AnomalyHub.ClientMethod, payload, cancellationToken);
        await _hub.Clients.Group(anomaly.Symbol).SendAsync(AnomalyHub.ClientMethod, payload, cancellationToken);
    }
}
