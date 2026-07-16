using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Notifications.GetUnreadNotificationCount;

namespace Ortakare.Api.Features.Notifications;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(
    GetUnreadNotificationCountHandler getUnreadNotificationCountHandler) : ControllerBase
{
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResult<GetUnreadNotificationCountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var result = await getUnreadNotificationCountHandler.HandleAsync(cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}