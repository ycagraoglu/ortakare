namespace Ortakare.Api.Features.Storage.ValidateUpload;

public sealed record ValidateUploadResponse(
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