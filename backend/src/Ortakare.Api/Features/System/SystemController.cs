using Microsoft.AspNetCore.Mvc;
using Ortakare.Api.Features.System.Health;

namespace Ortakare.Api.Features.System;

[ApiController]
[Route("api/system")]
public sealed class SystemController(GetHealthHandler getHealthHandler) : ControllerBase
{
    [HttpGet("health")]
    [ProducesResponseType(typeof(Common.ApiResult<HealthResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var result = await getHealthHandler.HandleAsync(cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
