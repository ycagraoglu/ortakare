using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Notifications.CreateNotificationStreamToken;
using Ortakare.Api.Features.Notifications.DeleteNotification;
using Ortakare.Api.Features.Notifications.GetMyNotifications;
using Ortakare.Api.Features.Notifications.GetUnreadNotificationCount;
using Ortakare.Api.Features.Notifications.MarkAllNotificationsAsRead;
using Ortakare.Api.Features.Notifications.MarkNotificationAsRead;

namespace Ortakare.Api.Features.Notifications;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(
    GetUnreadNotificationCountHandler getUnreadNotificationCountHandler,
    GetMyNotificationsHandler getMyNotificationsHandler,
    MarkNotificationAsReadHandler markNotificationAsReadHandler,
    MarkAllNotificationsAsReadHandler markAllNotificationsAsReadHandler,
    DeleteNotificationHandler deleteNotificationHandler,
    CreateNotificationStreamTokenHandler createNotificationStreamTokenHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<GetMyNotificationsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] string? cursor,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await getMyNotificationsHandler.HandleAsync(
            cursor,
            pageSize,
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResult<GetUnreadNotificationCountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var result = await getUnreadNotificationCountHandler.HandleAsync(cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{notificationId:guid}/read")]
    [ProducesResponseType(typeof(ApiResult<MarkNotificationAsReadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAsRead(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var result = await markNotificationAsReadHandler.HandleAsync(
            notificationId,
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("read-all")]
    [ProducesResponseType(typeof(ApiResult<MarkAllNotificationsAsReadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var result = await markAllNotificationsAsReadHandler.HandleAsync(cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("stream-token")]
    [ProducesResponseType(typeof(ApiResult<CreateNotificationStreamTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult CreateStreamToken()
    {
        var result = createNotificationStreamTokenHandler.Handle();
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{notificationId:guid}")]
    [ProducesResponseType(typeof(ApiResult<DeleteNotificationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var result = await deleteNotificationHandler.HandleAsync(
            notificationId,
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
