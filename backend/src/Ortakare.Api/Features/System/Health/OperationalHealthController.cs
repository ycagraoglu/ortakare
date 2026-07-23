using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ortakare.Api.Features.System.Health;

[ApiController]
[Route("health")]
public sealed class OperationalHealthController(HealthCheckService healthCheckService) : ControllerBase
{
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Live()
    {
        Response.Headers.CacheControl = "no-store, no-cache";

        return Ok(new
        {
            status = "Healthy",
            timestampUtc = DateTime.UtcNow
        });
    }

    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store, no-cache";

        var report = await healthCheckService.CheckHealthAsync(
            registration => registration.Tags.Contains("ready"),
            cancellationToken);

        var response = new
        {
            status = report.Status.ToString(),
            totalDurationMilliseconds = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            timestampUtc = DateTime.UtcNow,
            checks = report.Entries
                .OrderBy(entry => entry.Key)
                .Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    durationMilliseconds = Math.Round(entry.Value.Duration.TotalMilliseconds, 2),
                    data = entry.Value.Data.Count == 0 ? null : entry.Value.Data
                })
        };

        return report.Status == HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
