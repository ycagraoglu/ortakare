using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Events.ReopenEvent;

public sealed class ReopenEventHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<ApiResult<ReopenEventResponse>> HandleAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var eventEntity = await dbContext.Events
            .SingleOrDefaultAsync(
                x => x.Id == eventId && x.OwnerUserId == currentUser.UserId,
                cancellationToken);

        if (eventEntity is null)
        {
            return ApiResult<ReopenEventResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        if (!eventEntity.UploadsEnabled)
        {
            eventEntity.UploadsEnabled = true;
            eventEntity.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ApiResult<ReopenEventResponse>.Success(
            new ReopenEventResponse(
                eventEntity.Id,
                eventEntity.UploadsEnabled,
                eventEntity.UpdatedAtUtc ?? eventEntity.CreatedAtUtc));
    }
}
