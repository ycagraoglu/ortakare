using Ortakare.Api.Common;
using Ortakare.Api.Features.Dashboard.GetOwnerStorageQuota;
using Ortakare.Api.Features.Photos.UploadPhoto;

namespace Ortakare.Api.Features.Storage.GetUploadPolicy;

public sealed class GetUploadPolicyHandler(IConfiguration configuration)
{
    private static readonly string[] SupportedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/heic"
    ];

    private static readonly string[] SupportedFileExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".heic"
    ];

    public ApiResult<GetUploadPolicyResponse> Handle()
    {
        var photoUploadOptions = configuration
            .GetSection(PhotoUploadOptions.SectionName)
            .Get<PhotoUploadOptions>() ?? new PhotoUploadOptions();
        var quotaOptions = configuration
            .GetSection(OwnerStorageQuotaOptions.SectionName)
            .Get<OwnerStorageQuotaOptions>() ?? new OwnerStorageQuotaOptions();

        return ApiResult<GetUploadPolicyResponse>.Success(
            new GetUploadPolicyResponse(
                SupportedContentTypes,
                SupportedFileExtensions,
                photoUploadOptions.MaxFileSizeBytes,
                StorageUploadPolicyService.MaxFilesPerRequest,
                quotaOptions.QuotaBytes,
                quotaOptions.WarningThresholdPercent,
                quotaOptions.CriticalThresholdPercent));
    }
}
