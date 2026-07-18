using System.Diagnostics;
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
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var storageOptions = options.Value;
            await amazonS3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = storageOptions.BucketName,
                MaxKeys = 1
            }, cancellationToken);

            stopwatch.Stop();
            return HealthCheckResult.Healthy(
                "Cloudflare R2 bucket erişimi başarılı.",
                new Dictionary<string, object>
                {
                    ["elapsedMilliseconds"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
                    ["bucket"] = storageOptions.BucketName
                });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Cloudflare R2 sağlık kontrolü zaman aşımına uğradı.");
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            return HealthCheckResult.Unhealthy(
                "Cloudflare R2 erişilemiyor.",
                exception,
                new Dictionary<string, object>
                {
                    ["elapsedMilliseconds"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)
                });
        }
    }
}
