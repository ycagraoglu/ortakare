using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Participants.UpdateParticipantDisplayName;

public sealed class UpdateParticipantDisplayNameHandler(
    OrtakareDbContext dbContext,
    ParticipantTokenService participantTokenService)
{
    public async Task<ApiResult<UpdateParticipantDisplayNameResponse>> HandleAsync(
        string galleryToken,
        string participantToken,
        UpdateParticipantDisplayNameRequest request,
        CancellationToken cancellationToken)
    {
        var tokenHash = participantTokenService.Hash(participantToken);

        var participant = await dbContext.EventGuestParticipants
            .Join(
                dbContext.Events,
                participant => participant.EventId,
                eventEntity => eventEntity.Id,
                (participant, eventEntity) => new { Participant = participant, Event = eventEntity })
            .Where(x =>
                x.Event.GalleryToken == galleryToken &&
                x.Participant.TokenHash == tokenHash)
            .Select(x => x.Participant)
            .SingleOrDefaultAsync(cancellationToken);

        if (participant is null)
        {
            return ApiResult<UpdateParticipantDisplayNameResponse>.Failure(
                "Katılımcı doğrulanamadı.",
                StatusCodes.Status401Unauthorized);
        }

        participant.DisplayName = request.DisplayName.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<UpdateParticipantDisplayNameResponse>.Success(
            new UpdateParticipantDisplayNameResponse(
                participant.Id,
                participant.DisplayName),
            "Görünen ad güncellendi.");
    }
}
