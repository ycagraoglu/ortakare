using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Infrastructure.HealthChecks;

public sealed class PostgreSqlHealthCheck(
    OrtakareDbContext dbContext,
    IOptions<HealthCheckOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(options.Value.DependencyTimeoutSeconds));

        try
        {
            var connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync(timeoutSource.Token);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = options.Value.DependencyTimeoutSeconds;
            await command.ExecuteScalarAsync(timeoutSource.Token);

            await connection.CloseAsync();
            stopwatch.Stop();

            return HealthCheckResult.Healthy(
                "PostgreSQL bağlantısı ve sorgu yürütme başarılı.",
                new Dictionary<string, object>
                {
                    ["elapsedMilliseconds"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
                    ["database"] = connection.Database
                });
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            stopwatch.Stop();
            return HealthCheckResult.Unhealthy(
                "PostgreSQL sağlık kontrolü zaman aşımına uğradı.",
                data: new Dictionary<string, object>
                {
                    ["elapsedMilliseconds"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
                    ["timeoutSeconds"] = options.Value.DependencyTimeoutSeconds
                });
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            return HealthCheckResult.Unhealthy(
                "PostgreSQL erişilemiyor.",
                exception,
                new Dictionary<string, object>
                {
                    ["elapsedMilliseconds"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)
                });
        }
    }
}
