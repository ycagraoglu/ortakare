using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.GalleryExports.GetGalleryExportDownloadUrl;

public sealed class GetGalleryExportDownloadUrlHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    IObjectStorageService objectStorageService,
    IOptions<ObjectStorageOptions> objectStorageOptions,
    TimeProvider timeProvider)
{
    public async Task<ApiResult<GetGalleryExportDownloadUrlResponse>> HandleAsync(
        Guid eventId,
        Guid exportId,
        CancellationToken cancellationToken)
    {
        var galleryExport = await dbContext.GalleryExports
            .AsNoTracking()
            .Where(x => x.Id == exportId && x.EventId == eventId)
            .Where(x => dbContext.Events.Any(e => e.Id == x.EventId && e.OwnerUserId == currentUser.UserId))
            .Select(x => new
            {
                x.Id,
                x.Status,
                x.StorageKey
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (galleryExport is null)
        {
            return ApiResult<GetGalleryExportDownloadUrlResponse>.Failure(
                "Dışa aktarma kaydı bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        if (galleryExport.Status != GalleryExportStatus.Completed)
        {
            return ApiResult<GetGalleryExportDownloadUrlResponse>.Failure(
                "Dışa aktarma henüz indirilmeye hazır değil.",
                StatusCodes.Status409Conflict);
        }

        if (string.IsNullOrWhiteSpace(galleryExport.StorageKey))
        {
            return ApiResult<GetGalleryExportDownloadUrlResponse>.Failure(
                "Dışa aktarma dosyası bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var expiresAtUtc = timeProvider.GetUtcNow().UtcDateTime
            .AddMinutes(objectStorageOptions.Value.SignedUrlMinutes);
        var downloadUrl = objectStorageService.CreateReadUrl(
            galleryExport.StorageKey,
            expiresAtUtc);

        return ApiResult<GetGalleryExportDownloadUrlResponse>.Success(
            new GetGalleryExportDownloadUrlResponse(
                galleryExport.Id,
                downloadUrl,
                expiresAtUtc));
    }
}
