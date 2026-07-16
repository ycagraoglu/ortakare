using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ortakare.Api.Common;
using Ortakare.Api.Features.EventAudit.GetEventAuditLogs;

namespace Ortakare.Api.Features.EventAudit;

[ApiController]
[Authorize]
[Route("api/events/{eventId:guid}/audit-logs")]
public sealed class EventAuditController(
    GetEventAuditLogsHandler getEventAuditLogsHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<GetEventAuditLogsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        Guid eventId,
        [FromQuery] GetEventAuditLogsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await getEventAuditLogsHandler.HandleAsync(eventId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
