using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Events.DomainEvents;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.DomainEvents;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Events.CloseEvent;

public sealed class CloseEventHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<ApiResult<CloseEventResponse>> HandleAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var eventEntity = await dbContext.Events
            .SingleOrDefaultAsync(
                x => x.Id == eventId && x.OwnerUserId == currentUser.UserId,
                cancellationToken);

        if (eventEntity is null)
        {
            return ApiResult<CloseEventResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        if (eventEntity.UploadsEnabled)
        {
            var occurredAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            eventEntity.UploadsEnabled = false;
            eventEntity.UpdatedAtUtc = occurredAtUtc;

            await domainEventDispatcher.PublishAsync(
                new EventClosedDomainEvent(
                    eventEntity.Id,
                    currentUser.UserId,
                    occurredAtUtc),
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ApiResult<CloseEventResponse>.Success(
            new CloseEventResponse(
                eventEntity.Id,
                eventEntity.UploadsEnabled,
                eventEntity.UpdatedAtUtc ?? eventEntity.CreatedAtUtc));
    }
}
