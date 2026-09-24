using MarketPulse.Application.Contracts;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Mapping;
using MarketPulse.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace MarketPulse.Api.Controllers;

[ApiController]
[Route("api/v1/stocks")]
public sealed class StocksController : ControllerBase
{
    private readonly IMarketFeatureStore _features;
    private readonly IAnomalyStore _anomalies;
    private readonly IBarHistoryStore _bars;

    public StocksController(IMarketFeatureStore features, IAnomalyStore anomalies, IBarHistoryStore bars)
    {
        _features = features;
        _anomalies = anomalies;
        _bars = bars;
    }

    [HttpGet("{symbol}/snapshot")]
    public async Task<ActionResult<StockSnapshotDto>> Snapshot(string symbol, CancellationToken cancellationToken)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        var tick = await _features.GetLatestTickAsync(symbol, cancellationToken);
        var features = await _features.GetFeaturesAsync(symbol, cancellationToken);
        var anomaly = await _anomalies.GetLatestBySymbolAsync(symbol, cancellationToken);

        if (tick is null && features is null && anomaly is null)
        {
            return NotFound();
        }

        return Ok(new StockSnapshotDto
        {
            Symbol = symbol,
            LatestBar = tick is null ? null : ApiDtoMapper.ToDto(tick),
            Features = features is null ? null : ApiDtoMapper.ToDto(features),
            LatestAnomaly = anomaly is null ? null : ApiDtoMapper.ToDto(anomaly),
            Note = "Score is an activity metric, not a forecast or a trade recommendation."
        });
    }

    [HttpGet("{symbol}/features")]
    public async Task<ActionResult<FeatureDto>> Features(string symbol, CancellationToken cancellationToken)
    {
        var features = await _features.GetFeaturesAsync(symbol.Trim().ToUpperInvariant(), cancellationToken);
        return features is null ? NotFound() : Ok(ApiDtoMapper.ToDto(features));
    }

    [HttpGet("{symbol}/history")]
    public async Task<ActionResult<PagedResponse<BarDto>>> History(
        string symbol,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = HistoryQuery.Normalize(symbol, from, to, page, pageSize);
        var (items, total) = await _bars.SearchAsync(query, cancellationToken);
        return Ok(new PagedResponse<BarDto>
        {
            Page = query.Page,
            PageSize = query.PageSize,
            Total = total,
            Items = items
        });
    }
}
