using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Notifications.GetMyNotifications;

public sealed class GetMyNotificationsHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser)
{
    public async Task<ApiResult<GetMyNotificationsResponse>> HandleAsync(
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (pageSize is < 1 or > 50)
        {
            return ApiResult<GetMyNotificationsResponse>.Failure(
                "Sayfa boyutu 1 ile 50 arasında olmalıdır.",
                StatusCodes.Status400BadRequest);
        }

        NotificationCursor? decodedCursor = null;
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            if (!NotificationCursor.TryDecode(cursor, out var parsedCursor))
            {
                return ApiResult<GetMyNotificationsResponse>.Failure(
                    "Geçersiz bildirim cursor değeri.",
                    StatusCodes.Status400BadRequest);
            }

            decodedCursor = parsedCursor;
        }

        var query =
            from notification in dbContext.Notifications.AsNoTracking()
            join eventEntity in dbContext.Events.AsNoTracking()
                on notification.EventId equals eventEntity.Id into eventGroup
            from eventEntity in eventGroup.DefaultIfEmpty()
            where notification.OwnerUserId == currentUser.UserId
            select new
            {
                Notification = notification,
                EventTitle = eventEntity == null ? null : eventEntity.Title
            };

        if (decodedCursor is { } value)
        {
            query = query.Where(x =>
                x.Notification.CreatedAtUtc < value.CreatedAtUtc ||
                (x.Notification.CreatedAtUtc == value.CreatedAtUtc &&
                 x.Notification.Id.CompareTo(value.NotificationId) < 0));
        }

        var rows = await query
            .OrderByDescending(x => x.Notification.CreatedAtUtc)
            .ThenByDescending(x => x.Notification.Id)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > pageSize;
        var pageRows = hasMore ? rows.Take(pageSize).ToList() : rows;

        var items = pageRows
            .Select(x => new MyNotificationItem(
                x.Notification.Id,
                x.Notification.EventId,
                x.EventTitle,
                x.Notification.Type,
                x.Notification.Severity,
                x.Notification.Title,
                x.Notification.Message,
                x.Notification.ActionUrl,
                x.Notification.DataJson,
                x.Notification.ReadAtUtc.HasValue,
                x.Notification.CreatedAtUtc,
                x.Notification.ReadAtUtc))
            .ToList();

        string? nextCursor = null;
        if (hasMore && pageRows.Count > 0)
        {
            var last = pageRows[^1].Notification;
            nextCursor = new NotificationCursor(last.CreatedAtUtc, last.Id).Encode();
        }

        return ApiResult<GetMyNotificationsResponse>.Success(
            new GetMyNotificationsResponse(items, nextCursor, hasMore));
    }
}