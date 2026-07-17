using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.GalleryExports.CreateGalleryExport;

public sealed class CreateGalleryExportHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IGalleryExportJobScheduler jobScheduler)
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

        var hasActiveExport = await dbContext.GalleryExports
            .AsNoTracking()
            .AnyAsync(
                x => x.EventId == eventId &&
                     (x.Status == GalleryExportStatus.Pending ||
                      x.Status == GalleryExportStatus.Processing),
                cancellationToken);

        if (hasActiveExport)
        {
            return ActiveExportConflict();
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

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsActiveExportUniqueViolation(exception))
        {
            dbContext.Entry(galleryExport).State = EntityState.Detached;
            return ActiveExportConflict();
        }

        jobScheduler.Enqueue(galleryExport.Id);

        return ApiResult<CreateGalleryExportResponse>.Success(
            new CreateGalleryExportResponse(
                galleryExport.Id,
                galleryExport.Status,
                galleryExport.PhotoCount,
                galleryExport.CreatedAtUtc),
            "Galeri dışa aktarma talebi oluşturuldu.",
            StatusCodes.Status202Accepted);
    }

    private static ApiResult<CreateGalleryExportResponse> ActiveExportConflict() =>
        ApiResult<CreateGalleryExportResponse>.Failure(
            "Bu etkinlik için devam eden bir dışa aktarma zaten bulunuyor.",
            StatusCodes.Status409Conflict);

    private static bool IsActiveExportUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: GalleryExportConfiguration.ActiveExportUniqueIndexName
        };
}
