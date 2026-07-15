using System.Diagnostics;
using System.Security.Claims;

namespace Ortakare.Api.Middleware;

public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var traceId = context.TraceIdentifier;
        var userId = context.User.FindFirstValue("sub");

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceId"] = traceId,
            ["UserId"] = userId
        }))
        {
            try
            {
                await next(context);
            }
            finally
            {
                var elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

                logger.LogInformation(
                    "HTTP request completed {Method} {Path} with {StatusCode} in {ElapsedMilliseconds:0.00} ms",
                    context.Request.Method,
                    context.Request.Path.Value,
                    context.Response.StatusCode,
                    elapsedMilliseconds);
            }
        }
    }
}
