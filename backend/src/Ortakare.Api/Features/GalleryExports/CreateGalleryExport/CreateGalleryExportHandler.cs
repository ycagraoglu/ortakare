using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.GalleryExports.CreateGalleryExport;

public sealed class CreateGalleryExportHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<ApiResult<CreateGalleryExportResponse>> HandleAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var ownsEvent = await dbContext.Events
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == eventId && x.OwnerUserId == currentUser.UserId,
                cancellationToken);

        if (!ownsEvent)
        {
            return ApiResult<CreateGalleryExportResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var photoCount = await dbContext.EventGuestPhotos
            .AsNoTracking()
            .CountAsync(x => x.EventId == eventId, cancellationToken);

        if (photoCount == 0)
        {
            return ApiResult<CreateGalleryExportResponse>.Failure(
                "Dışa aktarılacak fotoğraf bulunamadı.",
                StatusCodes.Status409Conflict);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var galleryExport = new GalleryExport
        {
            Id = Guid.CreateVersion7(),
            EventId = eventId,
            Status = GalleryExportStatus.Pending,
            PhotoCount = photoCount,
            CreatedAtUtc = now
        };

        dbContext.GalleryExports.Add(galleryExport);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<CreateGalleryExportResponse>.Success(
            new CreateGalleryExportResponse(
                galleryExport.Id,
                galleryExport.Status,
                galleryExport.PhotoCount,
                galleryExport.CreatedAtUtc),
            "Galeri dışa aktarma talebi oluşturuldu.",
            StatusCodes.Status202Accepted);
    }
}
