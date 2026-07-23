using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Infrastructure.BackgroundJobs;

public sealed class CleanupExpiredGalleryExportsJob(
    OrtakareDbContext dbContext,
    IObjectStorageService objectStorageService,
    IOptions<GalleryExportCleanupOptions> options,
    TimeProvider timeProvider,
    ILogger<CleanupExpiredGalleryExportsJob> logger)
{
    public async Task<GalleryExportCleanupResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var batchSize = options.Value.BatchSize;

        var expiredExports = await dbContext.GalleryExports
            .Where(x => x.Status == GalleryExportStatus.Completed)
            .Where(x => x.ExpiresAtUtc.HasValue && x.ExpiresAtUtc <= now)
            .OrderBy(x => x.ExpiresAtUtc)
            .ThenBy(x => x.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var deletedCount = 0;
        var failedCount = 0;

        foreach (var galleryExport in expiredExports)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (!string.IsNullOrWhiteSpace(galleryExport.StorageKey))
                {
                    await objectStorageService.DeleteAsync(
                        galleryExport.StorageKey,
                        cancellationToken);
                }

                dbContext.GalleryExports.Remove(galleryExport);
                await dbContext.SaveChangesAsync(cancellationToken);
                deletedCount++;

                logger.LogInformation(
                    "Expired gallery export {ExportId} for event {EventId} was cleaned up.",
                    galleryExport.Id,
                    galleryExport.EventId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failedCount++;
                dbContext.Entry(galleryExport).State = EntityState.Unchanged;

                logger.LogError(
                    exception,
                    "Expired gallery export cleanup failed for export {ExportId} in event {EventId}.",
                    galleryExport.Id,
                    galleryExport.EventId);
            }
        }

        return new GalleryExportCleanupResult(
            ScannedCount: expiredExports.Count,
            DeletedCount: deletedCount,
            FailedCount: failedCount);
    }
}

public sealed record GalleryExportCleanupResult(
    int ScannedCount,
    int DeletedCount,
    int FailedCount);
