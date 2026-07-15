using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Dashboard.GetOwnerDashboardSummary;

namespace Ortakare.Api.Features.Dashboard;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(
    GetOwnerDashboardSummaryHandler getOwnerDashboardSummaryHandler) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResult<GetOwnerDashboardSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await getOwnerDashboardSummaryHandler.HandleAsync(cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
