using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Infrastructure.BackgroundJobs;

public sealed class CleanupOrphanFilesJob(
    OrtakareDbContext dbContext,
    IObjectStorageService objectStorageService,
    IOptions<OrphanFileCleanupOptions> options,
    TimeProvider timeProvider,
    ILogger<CleanupOrphanFilesJob> logger)
{
    private static readonly string[] ManagedPrefixes = ["events/", "exports/"];

    public async Task<OrphanFileCleanupResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var cutoffUtc = timeProvider.GetUtcNow().UtcDateTime
            .AddHours(-settings.GracePeriodHours);

        var referencedPhotoKeys = await dbContext.EventGuestPhotos
            .AsNoTracking()
            .Select(x => x.StorageKey)
            .ToHashSetAsync(cancellationToken);

        var referencedExportKeys = await dbContext.GalleryExports
            .AsNoTracking()
            .Where(x => x.StorageKey != null)
            .Select(x => x.StorageKey!)
            .ToHashSetAsync(cancellationToken);

        var referencedKeys = new HashSet<string>(referencedPhotoKeys, StringComparer.Ordinal);
        referencedKeys.UnionWith(referencedExportKeys);

        var scannedCount = 0;
        var orphanCount = 0;
        var deletedCount = 0;
        var failedCount = 0;

        foreach (var prefix in ManagedPrefixes)
        {
            var objects = await objectStorageService.ListAsync(
                prefix,
                settings.MaxObjectsPerPrefix,
                cancellationToken);

            foreach (var item in objects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                scannedCount++;

                if (item.LastModifiedUtc > cutoffUtc || referencedKeys.Contains(item.Key))
                {
                    continue;
                }

                orphanCount++;

                if (deletedCount >= settings.MaxDeletesPerRun)
                {
                    continue;
                }

                try
                {
                    await objectStorageService.DeleteAsync(item.Key, cancellationToken);
                    deletedCount++;
                }
                catch (Exception exception)
                {
                    failedCount++;
                    logger.LogError(
                        exception,
                        "Orphan object deletion failed for {StorageKey}.",
                        item.Key);
                }
            }
        }

        return new OrphanFileCleanupResult(
            scannedCount,
            orphanCount,
            deletedCount,
            failedCount);
    }
}

public sealed record OrphanFileCleanupResult(
    int ScannedCount,
    int OrphanCount,
    int DeletedCount,
    int FailedCount);