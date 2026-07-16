using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Features.EventAudit;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Participants.UnblockEventParticipant;

public sealed class UnblockEventParticipantHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    EventAuditWriter auditWriter)
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
            participant.IsBlocked = false;
            participant.BlockedAtUtc = null;
            auditWriter.AddOwnerAction(
                eventId,
                currentUser.UserId,
                "ParticipantUnblocked",
                "Katılımcının etkinlik erişim engeli kaldırıldı.",
                "Participant",
                participant.Id);
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
