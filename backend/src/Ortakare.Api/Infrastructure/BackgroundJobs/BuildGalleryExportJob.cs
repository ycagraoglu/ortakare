using System.IO.Compression;
using System.Text.Json;
using Hangfire;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Features.Notifications;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;
using Ortakare.Api.Infrastructure.Realtime;

namespace Ortakare.Api.Infrastructure.BackgroundJobs;

[AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
[DisableConcurrentExecution(timeoutInSeconds: 3600)]
public sealed class BuildGalleryExportJob(
    OrtakareDbContext dbContext,
    IObjectStorageService objectStorageService,
    NotificationOutboxWriter notificationOutboxWriter,
    IRealtimePublisher realtimePublisher,
    TimeProvider timeProvider,
    ILogger<BuildGalleryExportJob> logger)
{
    private const int AutomaticRetryAttempts = 2;
    private const int ZipProgressMaximum = 90;
    private const int UploadProgress = 95;

    public async Task ExecuteAsync(
        Guid exportId,
        PerformContext? performContext,
        CancellationToken cancellationToken)
    {
        var galleryExport = await dbContext.GalleryExports
            .SingleOrDefaultAsync(x => x.Id == exportId, cancellationToken);

        if (galleryExport is null)
        {
            logger.LogWarning("Gallery export {ExportId} was not found.", exportId);
            return;
        }

        if (galleryExport.Status is GalleryExportStatus.Completed or GalleryExportStatus.Cancelled)
        {
            return;
        }

        if (galleryExport.Status != GalleryExportStatus.Pending)
        {
            logger.LogWarning(
                "Gallery export {ExportId} cannot start from status {Status}.",
                exportId,
                galleryExport.Status);
            return;
        }

        var eventInfo = await dbContext.Events
            .AsNoTracking()
            .Where(x => x.Id == galleryExport.EventId)
            .Select(x => new ExportEventInfo(x.OwnerUserId, x.Title))
            .SingleOrDefaultAsync(cancellationToken);

        if (eventInfo is null)
        {
            logger.LogWarning(
                "Event {EventId} for gallery export {ExportId} was not found.",
                galleryExport.EventId,
                exportId);
            return;
        }

        galleryExport.Status = GalleryExportStatus.Processing;
        galleryExport.FailedAtUtc = null;
        galleryExport.CancelledAtUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        await PublishProgressAsync(
            eventInfo.OwnerUserId,
            galleryExport,
            progress: 0,
            processedPhotoCount: 0,
            totalPhotoCount: null,
            stage: "Preparing",
            cancellationToken);

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

            await PublishProgressAsync(
                eventInfo.OwnerUserId,
                galleryExport,
                progress: 1,
                processedPhotoCount: 0,
                totalPhotoCount: photos.Count,
                stage: "Compressing",
                cancellationToken);

            await CreateZipAsync(
                tempFilePath,
                photos,
                async (processedCount, progress, token) =>
                {
                    await PublishProgressAsync(
                        eventInfo.OwnerUserId,
                        galleryExport,
                        progress,
                        processedCount,
                        photos.Count,
                        "Compressing",
                        token);
                },
                cancellationToken);

            await PublishProgressAsync(
                eventInfo.OwnerUserId,
                galleryExport,
                UploadProgress,
                photos.Count,
                photos.Count,
                "Uploading",
                cancellationToken);

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

            var completedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

            galleryExport.Status = GalleryExportStatus.Completed;
            galleryExport.StorageKey = storageKey;
            galleryExport.PhotoCount = photos.Count;
            galleryExport.CompletedAtUtc = completedAtUtc;
            galleryExport.FailedAtUtc = null;
            galleryExport.CancelledAtUtc = null;

            notificationOutboxWriter.AddOwnerNotification(
                eventInfo.OwnerUserId,
                galleryExport.EventId,
                "GalleryExportCompleted",
                "Galeri dışa aktarımı hazır",
                $"{eventInfo.EventTitle} etkinliğine ait {photos.Count} fotoğraflık ZIP dosyası hazırlandı.",
                completedAtUtc,
                new
                {
                    ExportId = galleryExport.Id,
                    PhotoCount = photos.Count,
                    Status = galleryExport.Status.ToString()
                },
                NotificationSeverities.Success,
                $"/events/{galleryExport.EventId}/exports/{galleryExport.Id}");

            await dbContext.SaveChangesAsync(cancellationToken);

            await PublishProgressAsync(
                eventInfo.OwnerUserId,
                galleryExport,
                progress: 100,
                processedPhotoCount: photos.Count,
                totalPhotoCount: photos.Count,
                stage: "Completed",
                cancellationToken);
        }
        catch (Exception exception)
        {
            var retryCount = performContext?.GetJobParameter<int>("RetryCount") ?? 0;
            var isFinalAttempt = retryCount >= AutomaticRetryAttempts;
            var failedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

            logger.LogError(
                exception,
                "Gallery export {ExportId} failed. RetryCount: {RetryCount}, IsFinalAttempt: {IsFinalAttempt}.",
                exportId,
                retryCount,
                isFinalAttempt);

            galleryExport.Status = isFinalAttempt
                ? GalleryExportStatus.Failed
                : GalleryExportStatus.Pending;
            galleryExport.CompletedAtUtc = null;
            galleryExport.CancelledAtUtc = null;
            galleryExport.FailedAtUtc = isFinalAttempt ? failedAtUtc : null;

            if (isFinalAttempt)
            {
                notificationOutboxWriter.AddOwnerNotification(
                    eventInfo.OwnerUserId,
                    galleryExport.EventId,
                    "GalleryExportFailed",
                    "Galeri dışa aktarımı başarısız",
                    $"{eventInfo.EventTitle} etkinliğinin ZIP dosyası hazırlanamadı.",
                    failedAtUtc,
                    new
                    {
                        ExportId = galleryExport.Id,
                        Status = GalleryExportStatus.Failed.ToString()
                    },
                    NotificationSeverities.Error,
                    $"/events/{galleryExport.EventId}/exports/{galleryExport.Id}");
            }

            await dbContext.SaveChangesAsync(CancellationToken.None);

            await PublishProgressAsync(
                eventInfo.OwnerUserId,
                galleryExport,
                progress: 0,
                processedPhotoCount: 0,
                totalPhotoCount: null,
                stage: isFinalAttempt ? "Failed" : "Retrying",
                CancellationToken.None);

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
        Func<int, int, CancellationToken, Task> progressCallback,
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
        var lastPublishedProgress = 0;

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

            var processedCount = index + 1;
            var progress = Math.Max(1, processedCount * ZipProgressMaximum / photos.Count);

            if (progress >= lastPublishedProgress + 5 || processedCount == photos.Count)
            {
                lastPublishedProgress = progress;
                await progressCallback(processedCount, progress, cancellationToken);
            }
        }
    }

    private ValueTask PublishProgressAsync(
        Guid ownerUserId,
        GalleryExport galleryExport,
        int progress,
        int processedPhotoCount,
        int? totalPhotoCount,
        string stage,
        CancellationToken cancellationToken)
    {
        var occurredAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var payloadJson = JsonSerializer.Serialize(new
        {
            exportId = galleryExport.Id,
            eventId = galleryExport.EventId,
            status = galleryExport.Status.ToString(),
            progress,
            processedPhotoCount,
            totalPhotoCount,
            stage,
            occurredAtUtc
        });

        return realtimePublisher.PublishAsync(
            ownerUserId,
            new RealtimeEvent(
                Type: "GalleryExportProgress",
                PayloadJson: payloadJson,
                EventId: galleryExport.Id.ToString(),
                OccurredAtUtc: occurredAtUtc),
            cancellationToken);
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
    private sealed record ExportEventInfo(Guid OwnerUserId, string EventTitle);
}