namespace Ortakare.Api.Features.Dashboard.GetOwnerStorageQuota;

public sealed record GetOwnerStorageQuotaResponse(
    long QuotaBytes,
    long UsedBytes,
    long RemainingBytes,
    long OverQuotaBytes,
    decimal UsagePercent,
    OwnerStorageQuotaStatus Status,
    bool IsWarningThresholdReached,
    bool IsCriticalThresholdReached,
    bool IsQuotaExceeded);

public enum OwnerStorageQuotaStatus
{
    Healthy = 1,
    Warning = 2,
    Critical = 3,
    Exceeded = 4
}
