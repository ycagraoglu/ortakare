using Microsoft.Extensions.Diagnostics.HealthChecks;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Infrastructure.HealthChecks;

public sealed class PostgreSqlHealthCheck(OrtakareDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("PostgreSQL erişilebilir.")
                : HealthCheckResult.Unhealthy("PostgreSQL bağlantısı kurulamadı.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL erişilemiyor.", exception);
        }
    }
}
