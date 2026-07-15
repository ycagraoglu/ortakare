using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.GalleryExports.CancelPendingGalleryExport;

public sealed class CancelPendingGalleryExportHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<ApiResult<CancelPendingGalleryExportResponse>> HandleAsync(
        Guid eventId,
        Guid exportId,
        CancellationToken cancellationToken)
    {
        var galleryExport = await dbContext.GalleryExports
            .Where(x => x.Id == exportId && x.EventId == eventId)
            .Where(x => dbContext.Events.Any(e => e.Id == x.EventId && e.OwnerUserId == currentUser.UserId))
            .SingleOrDefaultAsync(cancellationToken);

        if (galleryExport is null)
        {
            return ApiResult<CancelPendingGalleryExportResponse>.Failure(
                "Dışa aktarma kaydı bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        if (galleryExport.Status == GalleryExportStatus.Cancelled)
        {
            return ApiResult<CancelPendingGalleryExportResponse>.Success(
                new CancelPendingGalleryExportResponse(
                    galleryExport.Id,
                    galleryExport.Status,
                    galleryExport.CancelledAtUtc!.Value),
                "Dışa aktarma talebi zaten iptal edilmiş.");
        }

        if (galleryExport.Status != GalleryExportStatus.Pending)
        {
            return ApiResult<CancelPendingGalleryExportResponse>.Failure(
                "Yalnızca henüz başlamamış dışa aktarma talepleri iptal edilebilir.",
                StatusCodes.Status409Conflict);
        }

        galleryExport.Status = GalleryExportStatus.Cancelled;
        galleryExport.CancelledAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        galleryExport.CompletedAtUtc = null;
        galleryExport.FailedAtUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<CancelPendingGalleryExportResponse>.Success(
            new CancelPendingGalleryExportResponse(
                galleryExport.Id,
                galleryExport.Status,
                galleryExport.CancelledAtUtc.Value),
            "Dışa aktarma talebi iptal edildi.");
    }
}
