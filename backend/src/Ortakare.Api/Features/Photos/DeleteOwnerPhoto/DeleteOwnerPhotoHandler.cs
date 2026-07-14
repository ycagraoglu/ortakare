using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Photos.DeleteOwnerPhoto;

public sealed class DeleteOwnerPhotoHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    IObjectStorageService objectStorageService)
{
    public async Task<ApiResult<DeleteOwnerPhotoResponse>> HandleAsync(
        Guid eventId,
        Guid photoId,
        CancellationToken cancellationToken)
    {
        var eventOwnedByUser = await dbContext.Events
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == eventId && x.OwnerUserId == currentUser.UserId,
                cancellationToken);

        if (!eventOwnedByUser)
        {
            return ApiResult<DeleteOwnerPhotoResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var photo = await dbContext.EventGuestPhotos
            .SingleOrDefaultAsync(
                x => x.Id == photoId && x.EventId == eventId,
                cancellationToken);

        if (photo is null)
        {
            return ApiResult<DeleteOwnerPhotoResponse>.Failure(
                "Fotoğraf bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        await objectStorageService.DeleteAsync(photo.StorageKey, cancellationToken);

        dbContext.EventGuestPhotos.Remove(photo);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<DeleteOwnerPhotoResponse>.Success(
            new DeleteOwnerPhotoResponse(photo.Id),
            "Fotoğraf silindi.");
    }
}