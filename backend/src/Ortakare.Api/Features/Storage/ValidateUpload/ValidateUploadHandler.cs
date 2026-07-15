using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Dashboard.GetOwnerStorageQuota;
using Ortakare.Api.Features.Photos.UploadPhoto;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Storage.ValidateUpload;

public sealed class ValidateUploadHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    IConfiguration configuration)
{
    private const int MaxFilesPerRequest = 100;

    public async Task<ApiResult<ValidateUploadResponse>> HandleAsync(
        ValidateUploadRequest request,
        CancellationToken cancellationToken)
    {
        var eventInfo = await dbContext.Events
            .AsNoTracking()
            .Where(x => x.Id == request.EventId && x.OwnerUserId == currentUser.UserId)
            .Select(x => new { x.Id, x.UploadsEnabled, x.OwnerUserId })
            .SingleOrDefaultAsync(cancellationToken);

        if (eventInfo is null)
        {
            return ApiResult<ValidateUploadResponse>.Failure(
                "Etkinlik bulunamadı.",
                StatusCodes.Status404NotFound);
        }

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
                @event.OwnerUserId == eventInfo.OwnerUserId))
            .SumAsync(photo => (long?)photo.FileSizeBytes, cancellationToken) ?? 0;

        var projectedUsageBytes = checked(currentUsageBytes + request.TotalBytes);
        var projectedUsagePercent = Math.Round(
            projectedUsageBytes * 100m / quotaOptions.QuotaBytes,
            2,
            MidpointRounding.AwayFromZero);
        var remainingBytes = Math.Max(0, quotaOptions.QuotaBytes - currentUsageBytes);

        string? message = null;
        var canUpload = true;

        if (!eventInfo.UploadsEnabled)
        {
            canUpload = false;
            message = "Bu albüm yeni yüklemelere kapatıldı.";
        }
        else if (request.FileCount > MaxFilesPerRequest)
        {
            canUpload = false;
            message = $"Tek istekte en fazla {MaxFilesPerRequest} dosya doğrulanabilir.";
        }
        else if (request.LargestFileBytes > photoUploadOptions.MaxFileSizeBytes)
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

        return ApiResult<ValidateUploadResponse>.Success(
            new ValidateUploadResponse(
                canUpload,
                quotaStatus,
                quotaOptions.QuotaBytes,
                currentUsageBytes,
                remainingBytes,
                request.TotalBytes,
                projectedUsageBytes,
                projectedUsagePercent,
                photoUploadOptions.MaxFileSizeBytes,
                MaxFilesPerRequest,
                message));
    }
}