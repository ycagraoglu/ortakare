using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ortakare.Api.Infrastructure.HealthChecks;

public static class HealthCheckResponseWriter
{
    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store, no-cache";

        var response = new
        {
            status = report.Status.ToString(),
            totalDurationMilliseconds = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            timestampUtc = DateTime.UtcNow,
            checks = report.Entries
                .OrderBy(entry => entry.Key)
                .Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    durationMilliseconds = Math.Round(entry.Value.Duration.TotalMilliseconds, 2),
                    data = entry.Value.Data.Count == 0 ? null : entry.Value.Data
                })
        };

        await context.Response.WriteAsJsonAsync(
            response,
            cancellationToken: context.RequestAborted);
    }
}
