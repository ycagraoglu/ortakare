using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.GalleryExports.RetryFailedGalleryExport;

public sealed class RetryFailedGalleryExportHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    IGalleryExportJobScheduler jobScheduler)
{
    public async Task<ApiResult<RetryFailedGalleryExportResponse>> HandleAsync(
        Guid eventId,
        Guid exportId,
        CancellationToken cancellationToken)
    {
        var galleryExport = await (
            from export in dbContext.GalleryExports
            join eventEntity in dbContext.Events
                on export.EventId equals eventEntity.Id
            where export.Id == exportId
                  && export.EventId == eventId
                  && eventEntity.OwnerUserId == currentUser.UserId
            select export)
            .SingleOrDefaultAsync(cancellationToken);

        if (galleryExport is null)
        {
            return ApiResult<RetryFailedGalleryExportResponse>.Failure(
                "Dışa aktarma kaydı bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        if (galleryExport.Status != GalleryExportStatus.Failed)
        {
            return ApiResult<RetryFailedGalleryExportResponse>.Failure(
                "Yalnızca başarısız dışa aktarma işlemleri yeniden denenebilir.",
                StatusCodes.Status409Conflict);
        }

        galleryExport.Status = GalleryExportStatus.Pending;
        galleryExport.FailedAtUtc = null;
        galleryExport.CompletedAtUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        jobScheduler.Enqueue(galleryExport.Id);

        return ApiResult<RetryFailedGalleryExportResponse>.Success(
            new RetryFailedGalleryExportResponse(
                galleryExport.Id,
                galleryExport.Status,
                galleryExport.PhotoCount,
                galleryExport.CreatedAtUtc),
            "Dışa aktarma işlemi yeniden kuyruğa alındı.",
            StatusCodes.Status202Accepted);
    }
}
