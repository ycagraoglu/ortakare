using System.IO.Compression;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Infrastructure.BackgroundJobs;

[AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
[DisableConcurrentExecution(timeoutInSeconds: 3600)]
public sealed class BuildGalleryExportJob(
    OrtakareDbContext dbContext,
    IObjectStorageService objectStorageService,
    TimeProvider timeProvider,
    ILogger<BuildGalleryExportJob> logger)
{
    public async Task ExecuteAsync(
        Guid exportId,
        CancellationToken cancellationToken)
    {
        var galleryExport = await dbContext.GalleryExports
            .SingleOrDefaultAsync(x => x.Id == exportId, cancellationToken);

        if (galleryExport is null)
        {
            logger.LogWarning("Gallery export {ExportId} was not found.", exportId);
            return;
        }

        if (galleryExport.Status == GalleryExportStatus.Completed)
        {
            return;
        }

        galleryExport.Status = GalleryExportStatus.Processing;
        galleryExport.FailedAtUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        var tempFilePath = Path.Combine(
            Path.GetTempPath(),
            $"ortakare-export-{galleryExport.Id:N}.zip");

        try
        {
            var photos = await dbContext.EventGuestPhotos
                .AsNoTracking()
                .Where(x => x.EventId == galleryExport.EventId)
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => new ExportPhoto(x.Id, x.StorageKey, x.ContentType))
                .ToListAsync(cancellationToken);

            if (photos.Count == 0)
            {
                throw new InvalidOperationException("Export edilecek fotoğraf bulunamadı.");
            }

            await CreateZipAsync(tempFilePath, photos, cancellationToken);

            var storageKey = $"exports/{galleryExport.EventId:N}/{galleryExport.Id:N}.zip";
            await using (var zipStream = new FileStream(
                tempFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true))
            {
                await objectStorageService.UploadAsync(
                    storageKey,
                    zipStream,
                    "application/zip",
                    zipStream.Length,
                    cancellationToken);
            }

            galleryExport.Status = GalleryExportStatus.Completed;
            galleryExport.StorageKey = storageKey;
            galleryExport.PhotoCount = photos.Count;
            galleryExport.CompletedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            galleryExport.FailedAtUtc = null;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Gallery export {ExportId} failed.", exportId);

            galleryExport.Status = GalleryExportStatus.Failed;
            galleryExport.CompletedAtUtc = null;
            galleryExport.FailedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            await dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            TryDeleteTempFile(tempFilePath);
        }
    }

    private async Task CreateZipAsync(
        string tempFilePath,
        IReadOnlyList<ExportPhoto> photos,
        CancellationToken cancellationToken)
    {
        await using var fileStream = new FileStream(
            tempFilePath,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen: true);

        for (var index = 0; index < photos.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var photo = photos[index];
            var entryName = $"{index + 1:D5}_{photo.Id:N}{GetExtension(photo.ContentType)}";
            var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);

            await using var source = await objectStorageService.OpenReadAsync(
                photo.StorageKey,
                cancellationToken);
            await using var target = entry.Open();
            await source.CopyToAsync(target, cancellationToken);
        }
    }

    private static string GetExtension(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/heic" => ".heic",
        _ => ".bin"
    };

    private static void TryDeleteTempFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Temporary file cleanup must not mask the export result.
        }
    }

    private sealed record ExportPhoto(Guid Id, string StorageKey, string ContentType);
}