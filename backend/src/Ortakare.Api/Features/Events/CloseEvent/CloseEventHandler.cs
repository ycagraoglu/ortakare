using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Features.EventAudit;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Events.CloseEvent;

public sealed class CloseEventHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    EventAuditWriter auditWriter)
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
            eventEntity.UploadsEnabled = false;
            eventEntity.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            auditWriter.AddOwnerAction(
                eventEntity.Id,
                currentUser.UserId,
                "EventClosed",
                "Etkinlik yeni yüklemelere kapatıldı.");
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ApiResult<CloseEventResponse>.Success(
            new CloseEventResponse(
                eventEntity.Id,
                eventEntity.UploadsEnabled,
                eventEntity.UpdatedAtUtc ?? eventEntity.CreatedAtUtc));
    }
}
