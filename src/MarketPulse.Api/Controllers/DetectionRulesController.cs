using MarketPulse.Application.Contracts;
using MarketPulse.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketPulse.Api.Controllers;

[ApiController]
[Route("api/v1/detection-rules")]
public sealed class DetectionRulesController : ControllerBase
{
    private readonly IDetectionRuleStore _store;

    public DetectionRulesController(IDetectionRuleStore store)
    {
        _store = store;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DetectionRuleDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _store.ListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<DetectionRuleDto>> Create([FromBody] UpsertDetectionRuleRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Version))
        {
            return ValidationProblem("Version is required.");
        }

        var created = await _store.CreateAsync(request, cancellationToken);
        return Created($"/api/v1/detection-rules/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DetectionRuleDto>> Update(
        Guid id,
        [FromBody] UpsertDetectionRuleRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _store.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }
}
