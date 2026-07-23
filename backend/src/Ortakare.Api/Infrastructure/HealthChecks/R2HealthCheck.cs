using System.Diagnostics;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Ortakare.Api.Infrastructure.ObjectStorage;

namespace Ortakare.Api.Infrastructure.HealthChecks;

public sealed class R2HealthCheck(
    IAmazonS3 amazonS3,
    IOptions<ObjectStorageOptions> storageOptions,
    IOptions<HealthCheckOptions> healthOptions) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(healthOptions.Value.DependencyTimeoutSeconds));

        try
        {
            var options = storageOptions.Value;
            await amazonS3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = options.BucketName,
                MaxKeys = 1
            }, timeoutSource.Token);

            stopwatch.Stop();
            return HealthCheckResult.Healthy(
                "Cloudflare R2 bucket erişimi başarılı.",
                new Dictionary<string, object>
                {
                    ["elapsedMilliseconds"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
                    ["bucket"] = options.BucketName
                });
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            stopwatch.Stop();
            return HealthCheckResult.Unhealthy(
                "Cloudflare R2 sağlık kontrolü zaman aşımına uğradı.",
                data: new Dictionary<string, object>
                {
                    ["elapsedMilliseconds"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
                    ["timeoutSeconds"] = healthOptions.Value.DependencyTimeoutSeconds
                });
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
