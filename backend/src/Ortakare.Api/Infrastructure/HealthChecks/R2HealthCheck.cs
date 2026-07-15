using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Ortakare.Api.Infrastructure.ObjectStorage;

namespace Ortakare.Api.Infrastructure.HealthChecks;

public sealed class R2HealthCheck(
    IAmazonS3 amazonS3,
    IOptions<ObjectStorageOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var storageOptions = options.Value;
            await amazonS3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = storageOptions.BucketName,
                MaxKeys = 1
            }, cancellationToken);

            return HealthCheckResult.Healthy("Cloudflare R2 erişilebilir.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Cloudflare R2 erişilemiyor.", exception);
        }
    }
}
