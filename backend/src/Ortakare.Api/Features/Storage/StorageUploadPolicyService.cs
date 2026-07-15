using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Dashboard.GetOwnerStorageQuota;
using Ortakare.Api.Features.Photos.UploadPhoto;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Storage;

public sealed class StorageUploadPolicyService(
    OrtakareDbContext dbContext,
    IConfiguration configuration)
{
    public const int MaxFilesPerRequest = 100;

    public async Task<StorageUploadPolicyDecision> EvaluateAsync(
        Guid ownerUserId,
        bool uploadsEnabled,
        int fileCount,
        long totalBytes,
        long largestFileBytes,
        CancellationToken cancellationToken)
    {
        var photoUploadOptions = configuration
            .GetSection(PhotoUploadOptions.SectionName)
            .Get<PhotoUploadOptions>() ?? new PhotoUploadOptions();
        var quotaOptions = configuration
            .GetSection(OwnerStorageQuotaOptions.SectionName)
            .Get<OwnerStorageQuotaOptions>() ?? new OwnerStorageQuotaOptions();

        var currentUsageBytes = await dbContext.EventGuestPhotos
            .AsNoTracking()
            .Where(photo => dbContext.Events.Any(@event =>
                @event.Id == photo.EventId &&
                @event.OwnerUserId == ownerUserId))
            .SumAsync(photo => (long?)photo.FileSizeBytes, cancellationToken) ?? 0;

        var projectedUsageBytes = checked(currentUsageBytes + totalBytes);
        var projectedUsagePercent = Math.Round(
            projectedUsageBytes * 100m / quotaOptions.QuotaBytes,
            2,
            MidpointRounding.AwayFromZero);
        var remainingBytes = Math.Max(0, quotaOptions.QuotaBytes - currentUsageBytes);

        string? message = null;
        var canUpload = true;

        if (!uploadsEnabled)
        {
            canUpload = false;
            message = "Bu albüm yeni yüklemelere kapatıldı.";
        }
        else if (fileCount > MaxFilesPerRequest)
        {
            canUpload = false;
            message = $"Tek istekte en fazla {MaxFilesPerRequest} dosya doğrulanabilir.";
        }
        else if (largestFileBytes > photoUploadOptions.MaxFileSizeBytes)
        {
            canUpload = false;
            message = "Seçilen dosyalardan en az biri izin verilen dosya boyutunu aşıyor.";
        }
        else if (projectedUsageBytes > quotaOptions.QuotaBytes)
        {
            canUpload = false;
            message = "Seçilen dosyalar depolama kotasını aşacaktır.";
        }

        var quotaStatus = projectedUsageBytes > quotaOptions.QuotaBytes
            ? "Exceeded"
            : projectedUsagePercent >= quotaOptions.CriticalThresholdPercent
                ? "Critical"
                : projectedUsagePercent >= quotaOptions.WarningThresholdPercent
                    ? "Warning"
                    : "Healthy";

        return new StorageUploadPolicyDecision(
            canUpload,
            quotaStatus,
            quotaOptions.QuotaBytes,
            currentUsageBytes,
            remainingBytes,
            totalBytes,
            projectedUsageBytes,
            projectedUsagePercent,
            photoUploadOptions.MaxFileSizeBytes,
            MaxFilesPerRequest,
            message);
    }
}

public sealed record StorageUploadPolicyDecision(
    bool CanUpload,
    string QuotaStatus,
    long QuotaBytes,
    long CurrentUsageBytes,
    long RemainingBytes,
    long RequestedBytes,
    long ProjectedUsageBytes,
    decimal ProjectedUsagePercent,
    long MaxFileSizeBytes,
    int MaxFilesPerRequest,
    string? Message);