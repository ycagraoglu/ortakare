using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ortakare.Api.Infrastructure.HealthChecks;

public static class HealthCheckResponseWriter
{
    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMilliseconds = Math.Round(entry.Value.Duration.TotalMilliseconds, 2)
            })
        };

        await context.Response.WriteAsJsonAsync(response, cancellationToken: context.RequestAborted);
    }
}
