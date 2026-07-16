using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Participants.DomainEvents;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.DomainEvents;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Participants.UnblockEventParticipant;

public sealed class UnblockEventParticipantHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<ApiResult<UnblockEventParticipantResponse>> HandleAsync(
        Guid eventId,
        Guid participantId,
        CancellationToken cancellationToken)
    {
        var ownsEvent = await dbContext.Events
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == eventId && x.OwnerUserId == currentUser.UserId,
                cancellationToken);

        if (!ownsEvent)
        {
            return ApiResult<UnblockEventParticipantResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var participant = await dbContext.EventGuestParticipants
            .SingleOrDefaultAsync(
                x => x.Id == participantId && x.EventId == eventId,
                cancellationToken);

        if (participant is null)
        {
            return ApiResult<UnblockEventParticipantResponse>.Failure(
                "Katılımcı bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        if (participant.IsBlocked)
        {
            var occurredAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            participant.IsBlocked = false;
            participant.BlockedAtUtc = null;

            await domainEventDispatcher.PublishAsync(
                new ParticipantUnblockedDomainEvent(
                    eventId,
                    currentUser.UserId,
                    participant.Id,
                    occurredAtUtc),
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ApiResult<UnblockEventParticipantResponse>.Success(
            new UnblockEventParticipantResponse(
                participant.Id,
                participant.IsBlocked,
                participant.BlockedAtUtc),
            "Katılımcının erişim engeli kaldırıldı.");
    }
}
