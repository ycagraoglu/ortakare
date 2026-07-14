using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Photos.DeleteGuestPhoto;

public sealed class DeleteGuestPhotoHandler(
    OrtakareDbContext dbContext,
    ParticipantTokenService participantTokenService,
    IObjectStorageService objectStorageService)
{
    public async Task<ApiResult<DeleteGuestPhotoResponse>> HandleAsync(
        string galleryToken,
        string participantToken,
        Guid photoId,
        CancellationToken cancellationToken)
    {
        var eventId = await dbContext.Events
            .AsNoTracking()
            .Where(x => x.GalleryToken == galleryToken)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (eventId is null)
        {
            return ApiResult<DeleteGuestPhotoResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        if (string.IsNullOrWhiteSpace(participantToken))
        {
            return ApiResult<DeleteGuestPhotoResponse>.Failure(
                "Katılımcı doğrulanamadı.",
                StatusCodes.Status401Unauthorized);
        }

        var tokenHash = participantTokenService.Hash(participantToken);
        var participantId = await dbContext.EventGuestParticipants
            .AsNoTracking()
            .Where(x => x.EventId == eventId.Value && x.TokenHash == tokenHash)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (participantId is null)
        {
            return ApiResult<DeleteGuestPhotoResponse>.Failure(
                "Katılımcı doğrulanamadı.",
                StatusCodes.Status401Unauthorized);
        }

        var photo = await dbContext.EventGuestPhotos
            .SingleOrDefaultAsync(
                x => x.Id == photoId &&
                     x.EventId == eventId.Value &&
                     x.ParticipantId == participantId.Value,
                cancellationToken);

        if (photo is null)
        {
            return ApiResult<DeleteGuestPhotoResponse>.Failure(
                "Fotoğraf bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        await objectStorageService.DeleteAsync(photo.StorageKey, cancellationToken);

        dbContext.EventGuestPhotos.Remove(photo);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<DeleteGuestPhotoResponse>.Success(
            new DeleteGuestPhotoResponse(photo.Id),
            "Fotoğraf silindi.");
    }
}
