using MarketPulse.Application.Contracts;
using MarketPulse.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketPulse.Api.Controllers;

[ApiController]
[Route("api/v1/replays")]
public sealed class ReplaysController : ControllerBase
{
    private readonly IHistoricalReplayService _replay;

    public ReplaysController(IHistoricalReplayService replay)
    {
        _replay = replay;
    }

    [HttpPost]
    public async Task<ActionResult<ReplayResultDto>> Start([FromBody] ReplayRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Source))
        {
            request.Source = "csv";
        }

        var result = await _replay.ReplayAsync(request, cancellationToken);
        return Ok(result);
    }
}
