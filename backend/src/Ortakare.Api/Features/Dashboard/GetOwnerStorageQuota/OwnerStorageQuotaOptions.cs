namespace Ortakare.Api.Features.Dashboard.GetOwnerStorageQuota;

public sealed class OwnerStorageQuotaOptions
{
    public const string SectionName = "OwnerStorageQuota";

    public long QuotaBytes { get; init; } = 100L * 1024 * 1024 * 1024;
    public decimal WarningThresholdPercent { get; init; } = 80m;
    public decimal CriticalThresholdPercent { get; init; } = 95m;
}
