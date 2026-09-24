using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MarketPulse.Api.Controllers;

[ApiController]
[Route("api/v1/system")]
public sealed class SystemController : ControllerBase
{
    private readonly HealthCheckService _health;

    public SystemController(HealthCheckService health)
    {
        _health = health;
    }

    [HttpGet("health")]
    public async Task<ActionResult<SystemHealthDto>> Health(CancellationToken cancellationToken)
    {
        var report = await _health.CheckHealthAsync(cancellationToken);
        return Ok(new SystemHealthDto
        {
            Status = report.Status.ToString(),
            TotalDurationMs = report.TotalDuration.TotalMilliseconds,
            Checks = report.Entries.Select(entry => new HealthCheckDto
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Description = entry.Value.Description,
                DurationMs = entry.Value.Duration.TotalMilliseconds
            }).ToList()
        });
    }
}

public sealed class SystemHealthDto
{
    public required string Status { get; init; }
    public required double TotalDurationMs { get; init; }
    public required IReadOnlyList<HealthCheckDto> Checks { get; init; }
}

public sealed class HealthCheckDto
{
    public required string Name { get; init; }
    public required string Status { get; init; }
    public string? Description { get; init; }
    public required double DurationMs { get; init; }
}
