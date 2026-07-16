using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Participants.DomainEvents;
using Ortakare.Api.Infrastructure.DomainEvents;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Participants.JoinEvent;

public sealed class JoinEventHandler(
    OrtakareDbContext dbContext,
    ParticipantTokenService participantTokenService,
    TimeProvider timeProvider,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<ApiResult<JoinEventResponse>> HandleAsync(
        string galleryToken,
        JoinEventRequest request,
        CancellationToken cancellationToken)
    {
        var eventInfo = await dbContext.Events
            .AsNoTracking()
            .Where(x => x.GalleryToken == galleryToken)
            .Select(x => new { x.Id, x.OwnerUserId, x.UploadsEnabled })
            .SingleOrDefaultAsync(cancellationToken);

        if (eventInfo is null)
        {
            return ApiResult<JoinEventResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        if (!eventInfo.UploadsEnabled)
        {
            return ApiResult<JoinEventResponse>.Failure(
                "Bu albüm yeni katılımlara ve yüklemelere kapatıldı.",
                StatusCodes.Status409Conflict);
        }

        var token = participantTokenService.Create();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var participant = new EventGuestParticipant
        {
            Id = Guid.CreateVersion7(),
            EventId = eventInfo.Id,
            DisplayName = request.DisplayName.Trim(),
            TokenHash = token.Hash,
            CreatedAtUtc = now
        };

        dbContext.EventGuestParticipants.Add(participant);

        await domainEventDispatcher.PublishAsync(
            new ParticipantJoinedDomainEvent(
                eventInfo.Id,
                eventInfo.OwnerUserId,
                participant.Id,
                now),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<JoinEventResponse>.Success(
            new JoinEventResponse(
                token.Value,
                participant.DisplayName,
                participant.CreatedAtUtc),
            "Albüme katılım oluşturuldu.",
            StatusCodes.Status201Created);
    }
}
