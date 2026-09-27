using MarketPulse.Application.Alerts;
using MarketPulse.Application.Contracts;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Options;
using MarketPulse.Domain.Alerts;
using MarketPulse.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MarketPulse.Api.Controllers;

[ApiController]
[Route("api/v1/alert-configurations")]
public sealed class AlertConfigurationsController : ControllerBase
{
    private readonly IAlertConfigurationStore _store;
    private readonly AlertOptions _options;

    public AlertConfigurationsController(IAlertConfigurationStore store, IOptions<AlertOptions> options)
    {
        _store = store;
        _options = options.Value;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AlertConfigurationDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _store.ListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AlertConfigurationDto>> Create(
        [FromBody] UpsertAlertConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryValidate(request, out var problem))
        {
            return problem;
        }

        var created = await _store.CreateAsync(request, cancellationToken);
        return Created($"/api/v1/alert-configurations/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AlertConfigurationDto>> Update(
        Guid id,
        [FromBody] UpsertAlertConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryValidate(request, out var problem))
        {
            return problem;
        }

        var updated = await _store.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _store.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private bool TryValidate(UpsertAlertConfigurationRequest request, out ActionResult problem)
    {
        problem = null!;
        if (!Enum.TryParse<AnomalySeverity>(request.MinSeverity, ignoreCase: true, out _))
        {
            problem = ValidationProblem("MinSeverity must be Informational, Elevated, High, or Extreme.");
            return false;
        }

        var channel = string.IsNullOrWhiteSpace(request.Channel) ? AlertMatcher.WebhookChannel : request.Channel;
        if (!string.Equals(channel, AlertMatcher.WebhookChannel, StringComparison.OrdinalIgnoreCase))
        {
            problem = ValidationProblem("Only the webhook channel is supported in this phase.");
            return false;
        }

        if (!AlertDestination.TryValidate(request.Destination, _options.AllowHttpLoopback, out _, out var error))
        {
            problem = ValidationProblem(error);
            return false;
        }

        return true;
    }
}
