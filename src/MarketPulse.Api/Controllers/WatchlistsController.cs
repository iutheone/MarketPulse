using MarketPulse.Application.Contracts;
using MarketPulse.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarketPulse.Api.Controllers;

[ApiController]
[Route("api/v1/watchlists")]
public sealed class WatchlistsController : ControllerBase
{
    private readonly IWatchlistStore _store;

    public WatchlistsController(IWatchlistStore store)
    {
        _store = store;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WatchlistDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _store.ListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<WatchlistDto>> Create([FromBody] CreateWatchlistRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ValidationProblem("Name is required.");
        }

        var symbols = (request.Symbols ?? [])
            .Select(s => s.Trim().ToUpperInvariant())
            .Where(s => s.Length > 0)
            .Distinct()
            .ToList();
        if (symbols.Count == 0)
        {
            return ValidationProblem("At least one symbol is required.");
        }

        var created = await _store.CreateAsync(request.Name, symbols, cancellationToken);
        return Created($"/api/v1/watchlists/{created.Id}", created);
    }
}
