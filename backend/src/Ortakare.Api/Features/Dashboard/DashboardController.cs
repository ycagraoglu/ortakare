using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Dashboard.GetOwnerDashboardSummary;
using Ortakare.Api.Features.Dashboard.GetOwnerRecentActivity;

namespace Ortakare.Api.Features.Dashboard;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(
    GetOwnerDashboardSummaryHandler getOwnerDashboardSummaryHandler,
    GetOwnerRecentActivityHandler getOwnerRecentActivityHandler) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResult<GetOwnerDashboardSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await getOwnerDashboardSummaryHandler.HandleAsync(cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("recent-activity")]
    [ProducesResponseType(typeof(ApiResult<GetOwnerRecentActivityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetRecentActivity(
        [FromQuery] GetOwnerRecentActivityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await getOwnerRecentActivityHandler.HandleAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
