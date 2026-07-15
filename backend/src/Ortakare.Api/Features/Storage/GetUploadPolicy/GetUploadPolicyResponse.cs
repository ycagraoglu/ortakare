namespace Ortakare.Api.Features.Storage.GetUploadPolicy;

public sealed record GetUploadPolicyResponse(
    IReadOnlyList<string> SupportedContentTypes,
    IReadOnlyList<string> SupportedFileExtensions,
    long MaxFileSizeBytes,
    int MaxFilesPerRequest,
    long DefaultQuotaBytes,
    decimal WarningThresholdPercent,
    decimal CriticalThresholdPercent);
