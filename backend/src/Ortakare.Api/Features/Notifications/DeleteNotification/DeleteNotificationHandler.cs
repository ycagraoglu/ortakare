using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Notifications.DeleteNotification;

public sealed class DeleteNotificationHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    OwnerUnreadCountOutboxWriter unreadCountOutboxWriter)
{
    public async Task<ApiResult<DeleteNotificationResponse>> HandleAsync(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var notification = await dbContext.Notifications
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                x => x.Id == notificationId && x.OwnerUserId == currentUser.UserId,
                cancellationToken);

        if (notification is null)
        {
            return ApiResult<DeleteNotificationResponse>.Failure(
                "Bildirim bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        if (notification.DeletedAtUtc is null)
        {
            var wasUnread = notification.ReadAtUtc is null;
            notification.DeletedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

            if (wasUnread)
            {
                unreadCountOutboxWriter.Add(currentUser.UserId);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ApiResult<DeleteNotificationResponse>.Success(
            new DeleteNotificationResponse(notification.Id, notification.DeletedAtUtc.Value));
    }
}
