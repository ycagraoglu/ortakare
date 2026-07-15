using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Events.DeleteEvent;

public sealed class DeleteEventHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    IObjectStorageService objectStorageService,
    ILogger<DeleteEventHandler> logger)
{
    public async Task<ApiResult<DeleteEventResponse>> HandleAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var eventEntity = await dbContext.Events
            .SingleOrDefaultAsync(
                x => x.Id == eventId && x.OwnerUserId == currentUser.UserId,
                cancellationToken);

        if (eventEntity is null)
        {
            return ApiResult<DeleteEventResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var hasActiveExport = await dbContext.GalleryExports
            .AsNoTracking()
            .AnyAsync(
                x => x.EventId == eventId
                    && (x.Status == GalleryExportStatus.Pending
                        || x.Status == GalleryExportStatus.Processing),
                cancellationToken);

        if (hasActiveExport)
        {
            return ApiResult<DeleteEventResponse>.Failure(
                "Etkinlik için devam eden bir toplu indirme işlemi bulunduğundan etkinlik silinemiyor.",
                StatusCodes.Status409Conflict);
        }

        var photoStorageKeys = await dbContext.EventGuestPhotos
            .AsNoTracking()
            .Where(x => x.EventId == eventId)
            .Select(x => x.StorageKey)
            .ToListAsync(cancellationToken);

        var exportStorageKeys = await dbContext.GalleryExports
            .AsNoTracking()
            .Where(x => x.EventId == eventId && x.StorageKey != null)
            .Select(x => x.StorageKey!)
            .ToListAsync(cancellationToken);

        var participantCount = await dbContext.EventGuestParticipants
            .AsNoTracking()
            .CountAsync(x => x.EventId == eventId, cancellationToken);

        var exportCount = await dbContext.GalleryExports
            .AsNoTracking()
            .CountAsync(x => x.EventId == eventId, cancellationToken);

        try
        {
            foreach (var storageKey in photoStorageKeys
                         .Concat(exportStorageKeys)
                         .Distinct(StringComparer.Ordinal))
            {
                await objectStorageService.DeleteAsync(storageKey, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Object storage cleanup failed while deleting event {EventId}.",
                eventId);

            return ApiResult<DeleteEventResponse>.Failure(
                "Etkinlik dosyaları silinemediği için işlem tamamlanamadı. Lütfen tekrar deneyin.",
                StatusCodes.Status503ServiceUnavailable);
        }

        dbContext.Events.Remove(eventEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<DeleteEventResponse>.Success(
            new DeleteEventResponse(
                eventId,
                photoStorageKeys.Count,
                participantCount,
                exportCount),
            "Etkinlik silindi.");
    }
}
