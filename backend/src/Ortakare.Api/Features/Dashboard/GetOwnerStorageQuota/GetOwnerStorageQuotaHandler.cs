using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Dashboard.GetOwnerStorageQuota;

public sealed class GetOwnerStorageQuotaHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    IOptions<OwnerStorageQuotaOptions> options)
{
    public async Task<ApiResult<GetOwnerStorageQuotaResponse>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var quotaOptions = options.Value;

        var usedBytes = await dbContext.EventGuestPhotos
            .AsNoTracking()
            .Where(photo => dbContext.Events.Any(@event =>
                @event.Id == photo.EventId &&
                @event.OwnerUserId == currentUser.UserId))
            .SumAsync(photo => (long?)photo.FileSizeBytes, cancellationToken)
            ?? 0;

        var remainingBytes = Math.Max(0, quotaOptions.QuotaBytes - usedBytes);
        var overQuotaBytes = Math.Max(0, usedBytes - quotaOptions.QuotaBytes);
        var usagePercent = quotaOptions.QuotaBytes == 0
            ? 0m
            : Math.Round(usedBytes * 100m / quotaOptions.QuotaBytes, 2, MidpointRounding.AwayFromZero);

        var isQuotaExceeded = usedBytes > quotaOptions.QuotaBytes;
        var isCriticalThresholdReached = usagePercent >= quotaOptions.CriticalThresholdPercent;
        var isWarningThresholdReached = usagePercent >= quotaOptions.WarningThresholdPercent;

        var status = isQuotaExceeded
            ? OwnerStorageQuotaStatus.Exceeded
            : isCriticalThresholdReached
                ? OwnerStorageQuotaStatus.Critical
                : isWarningThresholdReached
                    ? OwnerStorageQuotaStatus.Warning
                    : OwnerStorageQuotaStatus.Healthy;

        return ApiResult<GetOwnerStorageQuotaResponse>.Success(
            new GetOwnerStorageQuotaResponse(
                quotaOptions.QuotaBytes,
                usedBytes,
                remainingBytes,
                overQuotaBytes,
                usagePercent,
                status,
                isWarningThresholdReached,
                isCriticalThresholdReached,
                isQuotaExceeded));
    }
}
