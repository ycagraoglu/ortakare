using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Participants.DomainEvents;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.DomainEvents;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Participants.BlockEventParticipant;

public sealed class BlockEventParticipantHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<ApiResult<BlockEventParticipantResponse>> HandleAsync(
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
            return ApiResult<BlockEventParticipantResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var participant = await dbContext.EventGuestParticipants
            .SingleOrDefaultAsync(
                x => x.Id == participantId && x.EventId == eventId,
                cancellationToken);

        if (participant is null)
        {
            return ApiResult<BlockEventParticipantResponse>.Failure(
                "Katılımcı bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        if (!participant.IsBlocked)
        {
            var blockedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            participant.IsBlocked = true;
            participant.BlockedAtUtc = blockedAtUtc;

            await domainEventDispatcher.PublishAsync(
                new ParticipantBlockedDomainEvent(
                    eventId,
                    currentUser.UserId,
                    participant.Id,
                    blockedAtUtc),
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ApiResult<BlockEventParticipantResponse>.Success(
            new BlockEventParticipantResponse(
                participant.Id,
                participant.IsBlocked,
                participant.BlockedAtUtc!.Value),
            "Katılımcının erişimi engellendi.");
    }
}
