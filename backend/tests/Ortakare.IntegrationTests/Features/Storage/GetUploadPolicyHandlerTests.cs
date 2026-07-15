using Microsoft.Extensions.Configuration;
using Ortakare.Api.Features.Storage.GetUploadPolicy;

namespace Ortakare.IntegrationTests.Features.Storage;

public sealed class GetUploadPolicyHandlerTests
{
    [Fact]
    public void Handle_ReturnsConfiguredUploadPolicy()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PhotoUpload:MaxFileSizeBytes"] = "10485760",
                ["OwnerStorageQuota:QuotaBytes"] = "53687091200",
                ["OwnerStorageQuota:WarningThresholdPercent"] = "75",
                ["OwnerStorageQuota:CriticalThresholdPercent"] = "90"
            })
            .Build();

        var handler = new GetUploadPolicyHandler(configuration);

        var result = handler.Handle();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(10_485_760, result.Data.MaxFileSizeBytes);
        Assert.Equal(100, result.Data.MaxFilesPerRequest);
        Assert.Equal(53_687_091_200, result.Data.DefaultQuotaBytes);
        Assert.Equal(75m, result.Data.WarningThresholdPercent);
        Assert.Equal(90m, result.Data.CriticalThresholdPercent);
        Assert.Contains("image/jpeg", result.Data.SupportedContentTypes);
        Assert.Contains("image/heic", result.Data.SupportedContentTypes);
        Assert.Contains(".jpeg", result.Data.SupportedFileExtensions);
        Assert.Contains(".webp", result.Data.SupportedFileExtensions);
    }

    [Fact]
    public void Handle_UsesSafeDefaultsWhenConfigurationIsMissing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var handler = new GetUploadPolicyHandler(configuration);

        var result = handler.Handle();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(25L * 1024 * 1024, result.Data.MaxFileSizeBytes);
        Assert.Equal(100L * 1024 * 1024 * 1024, result.Data.DefaultQuotaBytes);
        Assert.Equal(80m, result.Data.WarningThresholdPercent);
        Assert.Equal(95m, result.Data.CriticalThresholdPercent);
        Assert.Equal(4, result.Data.SupportedContentTypes.Count);
        Assert.Equal(5, result.Data.SupportedFileExtensions.Count);
    }
}
