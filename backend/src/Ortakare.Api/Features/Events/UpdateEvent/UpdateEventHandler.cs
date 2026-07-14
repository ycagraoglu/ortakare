using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Events.UpdateEvent;

public sealed class UpdateEventHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<ApiResult<UpdateEventResponse>> HandleAsync(
        Guid eventId,
        UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        var eventEntity = await dbContext.Events
            .SingleOrDefaultAsync(
                x => x.Id == eventId && x.OwnerUserId == currentUser.UserId,
                cancellationToken);

        if (eventEntity is null)
        {
            return ApiResult<UpdateEventResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        eventEntity.Title = request.Title.Trim();
        eventEntity.EventDateUtc = request.EventDateUtc;
        eventEntity.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<UpdateEventResponse>.Success(
            new UpdateEventResponse(
                eventEntity.Id,
                eventEntity.Title,
                eventEntity.EventDateUtc,
                eventEntity.UploadsEnabled,
                eventEntity.UpdatedAtUtc.Value));
    }
}