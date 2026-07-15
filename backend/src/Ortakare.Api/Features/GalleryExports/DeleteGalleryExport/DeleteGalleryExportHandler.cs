using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.GalleryExports.DeleteGalleryExport;

public sealed class DeleteGalleryExportHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    IObjectStorageService objectStorageService,
    ILogger<DeleteGalleryExportHandler> logger)
{
    public async Task<ApiResult<DeleteGalleryExportResponse>> HandleAsync(
        Guid eventId,
        Guid exportId,
        CancellationToken cancellationToken)
    {
        var ownsEvent = await dbContext.Events
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == eventId && x.OwnerUserId == currentUser.UserId,
                cancellationToken);

        if (!ownsEvent)
        {
            return ApiResult<DeleteGalleryExportResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var galleryExport = await dbContext.GalleryExports
            .SingleOrDefaultAsync(
                x => x.Id == exportId && x.EventId == eventId,
                cancellationToken);

        if (galleryExport is null)
        {
            return ApiResult<DeleteGalleryExportResponse>.Failure(
                "Dışa aktarma kaydı bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        if (galleryExport.Status is GalleryExportStatus.Pending or GalleryExportStatus.Processing)
        {
            return ApiResult<DeleteGalleryExportResponse>.Failure(
                "Devam eden bir dışa aktarma işlemi silinemez.",
                StatusCodes.Status409Conflict);
        }

        var storageObjectDeleted = false;

        if (!string.IsNullOrWhiteSpace(galleryExport.StorageKey))
        {
            try
            {
                await objectStorageService.DeleteAsync(
                    galleryExport.StorageKey,
                    cancellationToken);
                storageObjectDeleted = true;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Gallery export storage cleanup failed for export {ExportId} in event {EventId}.",
                    exportId,
                    eventId);

                return ApiResult<DeleteGalleryExportResponse>.Failure(
                    "Dışa aktarma dosyası silinemediği için işlem tamamlanamadı. Lütfen tekrar deneyin.",
                    StatusCodes.Status503ServiceUnavailable);
            }
        }

        var status = galleryExport.Status;
        dbContext.GalleryExports.Remove(galleryExport);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<DeleteGalleryExportResponse>.Success(
            new DeleteGalleryExportResponse(
                exportId,
                status,
                storageObjectDeleted),
            "Dışa aktarma kaydı silindi.");
    }
}
