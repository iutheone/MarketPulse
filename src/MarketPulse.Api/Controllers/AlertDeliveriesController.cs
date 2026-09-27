using MarketPulse.Application.Contracts;
using MarketPulse.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketPulse.Api.Controllers;

[ApiController]
[Route("api/v1/alert-deliveries")]
public sealed class AlertDeliveriesController : ControllerBase
{
    private readonly IAlertDeliveryStore _store;

    public AlertDeliveriesController(IAlertDeliveryStore store)
    {
        _store = store;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AlertDeliveryDto>>> List(
        [FromQuery] Guid? anomalyId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        return Ok(await _store.ListAsync(anomalyId, status, cancellationToken));
    }
}
