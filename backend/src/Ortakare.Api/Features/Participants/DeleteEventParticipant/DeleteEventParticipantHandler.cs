using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Participants.DeleteEventParticipant;

public sealed class DeleteEventParticipantHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    IObjectStorageService objectStorageService,
    ILogger<DeleteEventParticipantHandler> logger)
{
    public async Task<ApiResult<DeleteEventParticipantResponse>> HandleAsync(
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
            return ApiResult<DeleteEventParticipantResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var participant = await dbContext.EventGuestParticipants
            .SingleOrDefaultAsync(
                x => x.Id == participantId && x.EventId == eventId,
                cancellationToken);

        if (participant is null)
        {
            return ApiResult<DeleteEventParticipantResponse>.Failure(
                "Katılımcı bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var photoStorageKeys = await dbContext.EventGuestPhotos
            .AsNoTracking()
            .Where(x => x.EventId == eventId && x.ParticipantId == participantId)
            .Select(x => x.StorageKey)
            .ToListAsync(cancellationToken);

        try
        {
            foreach (var storageKey in photoStorageKeys.Distinct(StringComparer.Ordinal))
            {
                await objectStorageService.DeleteAsync(storageKey, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Object storage cleanup failed while deleting participant {ParticipantId} from event {EventId}.",
                participantId,
                eventId);

            return ApiResult<DeleteEventParticipantResponse>.Failure(
                "Katılımcının fotoğrafları silinemediği için işlem tamamlanamadı. Lütfen tekrar deneyin.",
                StatusCodes.Status503ServiceUnavailable);
        }

        dbContext.EventGuestParticipants.Remove(participant);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<DeleteEventParticipantResponse>.Success(
            new DeleteEventParticipantResponse(participantId, photoStorageKeys.Count),
            "Katılımcı ve fotoğrafları silindi.");
    }
}
