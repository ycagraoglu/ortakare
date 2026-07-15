using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Dashboard.GetOwnerStorageQuota;

public sealed class GetOwnerStorageQuotaHandler(
    OrtakareDbContext dbContext,
    ICurrentUser currentUser,
    IConfiguration configuration)
{
    public async Task<ApiResult<GetOwnerStorageQuotaResponse>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var quotaOptions = configuration
            .GetSection(OwnerStorageQuotaOptions.SectionName)
            .Get<OwnerStorageQuotaOptions>()
            ?? new OwnerStorageQuotaOptions();

        ValidateOptions(quotaOptions);

        var usedBytes = await dbContext.EventGuestPhotos
            .AsNoTracking()
            .Where(photo => dbContext.Events.Any(@event =>
                @event.Id == photo.EventId &&
                @event.OwnerUserId == currentUser.UserId))
            .SumAsync(photo => (long?)photo.FileSizeBytes, cancellationToken)
            ?? 0;

        var remainingBytes = Math.Max(0, quotaOptions.QuotaBytes - usedBytes);
        var overQuotaBytes = Math.Max(0, usedBytes - quotaOptions.QuotaBytes);
        var usagePercent = Math.Round(
            usedBytes * 100m / quotaOptions.QuotaBytes,
            2,
            MidpointRounding.AwayFromZero);

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

    private static void ValidateOptions(OwnerStorageQuotaOptions options)
    {
        if (options.QuotaBytes <= 0)
        {
            throw new InvalidOperationException("OwnerStorageQuota:QuotaBytes must be greater than zero.");
        }

        if (options.WarningThresholdPercent is <= 0 or >= 100)
        {
            throw new InvalidOperationException("OwnerStorageQuota:WarningThresholdPercent must be between 0 and 100.");
        }

        if (options.CriticalThresholdPercent is <= 0 or > 100 ||
            options.CriticalThresholdPercent <= options.WarningThresholdPercent)
        {
            throw new InvalidOperationException("OwnerStorageQuota:CriticalThresholdPercent must be greater than warning threshold and at most 100.");
        }
    }
}
