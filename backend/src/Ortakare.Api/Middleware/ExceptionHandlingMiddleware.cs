using System.Text.Json;
using Ortakare.Api.Common;

namespace Ortakare.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var result = ApiResult.Failure(
                "Beklenmeyen bir hata oluştu.",
                StatusCodes.Status500InternalServerError);

            await context.Response.WriteAsync(JsonSerializer.Serialize(result));
        }
    }
}
