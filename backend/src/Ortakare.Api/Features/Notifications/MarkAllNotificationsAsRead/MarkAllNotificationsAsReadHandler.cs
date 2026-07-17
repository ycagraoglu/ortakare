using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Notifications.MarkAllNotificationsAsRead;

public sealed class MarkAllNotificationsAsReadHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    OwnerUnreadCountOutboxWriter unreadCountOutboxWriter)
{
    public async Task<ApiResult<MarkAllNotificationsAsReadResponse>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var readAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var updatedCount = await dbContext.Notifications
            .Where(x => x.OwnerUserId == currentUser.UserId && x.ReadAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.ReadAtUtc, readAtUtc),
                cancellationToken);

        if (updatedCount > 0)
        {
            unreadCountOutboxWriter.Add(currentUser.UserId);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return ApiResult<MarkAllNotificationsAsReadResponse>.Success(
            new MarkAllNotificationsAsReadResponse(updatedCount, readAtUtc),
            updatedCount > 0
                ? "Tüm bildirimler okundu olarak işaretlendi."
                : "Okunmamış bildirim bulunmuyor.");
    }
}
