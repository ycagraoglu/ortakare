using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Infrastructure.HealthChecks;

public sealed class PostgreSqlHealthCheck(OrtakareDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 2;
            await command.ExecuteScalarAsync(cancellationToken);

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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL sağlık kontrolü zaman aşımına uğradı.");
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
