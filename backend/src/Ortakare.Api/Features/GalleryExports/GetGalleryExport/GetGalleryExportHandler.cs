using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.GalleryExports.GetGalleryExport;

public sealed class GetGalleryExportHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    IObjectStorageService objectStorageService,
    IOptions<ObjectStorageOptions> objectStorageOptions,
    TimeProvider timeProvider)
{
    public async Task<ApiResult<GetGalleryExportResponse>> HandleAsync(
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
                x.PhotoCount,
                x.StorageKey,
                x.CreatedAtUtc,
                x.CompletedAtUtc,
                x.FailedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (galleryExport is null)
        {
            return ApiResult<GetGalleryExportResponse>.Failure(
                "Dışa aktarma kaydı bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        string? downloadUrl = null;
        DateTime? downloadUrlExpiresAtUtc = null;

        if (galleryExport.Status == GalleryExportStatus.Completed &&
            !string.IsNullOrWhiteSpace(galleryExport.StorageKey))
        {
            downloadUrlExpiresAtUtc = timeProvider.GetUtcNow().UtcDateTime
                .AddMinutes(objectStorageOptions.Value.SignedUrlMinutes);
            downloadUrl = objectStorageService.CreateReadUrl(
                galleryExport.StorageKey,
                downloadUrlExpiresAtUtc.Value);
        }

        return ApiResult<GetGalleryExportResponse>.Success(
            new GetGalleryExportResponse(
                galleryExport.Id,
                galleryExport.Status,
                galleryExport.PhotoCount,
                galleryExport.CreatedAtUtc,
                galleryExport.CompletedAtUtc,
                galleryExport.FailedAtUtc,
                downloadUrl,
                downloadUrlExpiresAtUtc));
    }
}
