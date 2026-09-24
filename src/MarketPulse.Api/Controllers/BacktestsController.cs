using MarketPulse.Application.Contracts;
using MarketPulse.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketPulse.Api.Controllers;

[ApiController]
[Route("api/v1/backtests")]
public sealed class BacktestsController : ControllerBase
{
    private readonly IBacktestService _backtests;

    public BacktestsController(IBacktestService backtests)
    {
        _backtests = backtests;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BacktestRunDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _backtests.ListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BacktestRunDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var run = await _backtests.GetByIdAsync(id, cancellationToken);
        return run is null ? NotFound() : Ok(run);
    }

    [HttpPost]
    public async Task<ActionResult<BacktestRunDto>> Run([FromBody] BacktestRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Source))
        {
            request.Source = "csv";
        }

        var run = await _backtests.RunAsync(request, cancellationToken);
        return Created($"/api/v1/backtests/{run.Id}", run);
    }
}
