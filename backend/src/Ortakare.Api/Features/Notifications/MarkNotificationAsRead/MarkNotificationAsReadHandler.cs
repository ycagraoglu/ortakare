using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Notifications.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<ApiResult<MarkNotificationAsReadResponse>> HandleAsync(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var notification = await dbContext.Notifications
            .SingleOrDefaultAsync(
                x => x.Id == notificationId && x.OwnerUserId == currentUser.UserId,
                cancellationToken);

        if (notification is null)
        {
            return ApiResult<MarkNotificationAsReadResponse>.Failure(
                "Bildirim bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        if (!notification.ReadAtUtc.HasValue)
        {
            notification.ReadAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ApiResult<MarkNotificationAsReadResponse>.Success(
            new MarkNotificationAsReadResponse(
                notification.Id,
                true,
                notification.ReadAtUtc.Value));
    }
}
