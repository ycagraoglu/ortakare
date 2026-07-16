using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Notifications.GetUnreadNotificationCount;

public sealed class GetUnreadNotificationCountHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser)
{
    public async Task<ApiResult<GetUnreadNotificationCountResponse>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var count = await dbContext.Notifications
            .AsNoTracking()
            .CountAsync(
                x => x.OwnerUserId == currentUser.UserId && x.ReadAtUtc == null,
                cancellationToken);

        return ApiResult<GetUnreadNotificationCountResponse>.Success(
            new GetUnreadNotificationCountResponse(count));
    }
}