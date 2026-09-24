using MarketPulse.Application.Contracts;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Mapping;
using MarketPulse.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace MarketPulse.Api.Controllers;

[ApiController]
[Route("api/v1/anomalies")]
public sealed class AnomaliesController : ControllerBase
{
    private readonly IAnomalyStore _store;

    public AnomaliesController(IAnomalyStore store)
    {
        _store = store;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AnomalyDto>>> List(
        [FromQuery] string? symbol,
        [FromQuery] string? severity,
        [FromQuery] string? sort,
        [FromQuery] string? direction,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = AnomalyQuery.Normalize(symbol, severity, sort, direction, page, pageSize);
        var (items, total) = await _store.SearchAsync(query, cancellationToken);
        return Ok(new PagedResponse<AnomalyDto>
        {
            Page = query.Page,
            PageSize = query.PageSize,
            Total = total,
            Items = items.Select(ApiDtoMapper.ToDto).ToList()
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AnomalyDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var anomaly = await _store.GetByIdAsync(id, cancellationToken);
        return anomaly is null ? NotFound() : Ok(ApiDtoMapper.ToDto(anomaly));
    }
}
