using System.Text.Json;
using Microsoft.Extensions.Options;
using Ortakare.Api.Common;

namespace Ortakare.Api.Features.Notifications.Streaming;

public sealed class StreamNotificationsHandler(
    INotificationStreamTokenService tokenService,
    INotificationSseConnectionManager connectionManager,
    IOptions<NotificationStreamOptions> options,
    TimeProvider timeProvider)
{
    public async Task HandleAsync(
        HttpContext httpContext,
        string? token,
        CancellationToken cancellationToken)
    {
        var tokenResult = tokenService.Consume(token ?? string.Empty);

        if (tokenResult is null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(
                ApiResult.Failure("Geçersiz veya süresi dolmuş stream tokenı.", StatusCodes.Status401Unauthorized),
                cancellationToken);
            return;
        }

        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        httpContext.Response.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache, no-store";
        httpContext.Response.Headers.Connection = "keep-alive";
        httpContext.Response.Headers.Append("X-Accel-Buffering", "no");

        await using var subscription = connectionManager.Subscribe(tokenResult.OwnerUserId);

        await WriteEventAsync(
            httpContext.Response,
            new NotificationSseEvent(
                "connected",
                JsonSerializer.Serialize(new
                {
                    connectionId = subscription.ConnectionId,
                    connectedAtUtc = timeProvider.GetUtcNow().UtcDateTime
                })),
            cancellationToken);

        var heartbeatInterval = TimeSpan.FromSeconds(options.Value.HeartbeatSeconds);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var readTask = subscription.Reader.WaitToReadAsync(cancellationToken).AsTask();
                var heartbeatTask = Task.Delay(heartbeatInterval, timeProvider, heartbeatCancellation.Token);
                var completedTask = await Task.WhenAny(readTask, heartbeatTask);

                if (completedTask == readTask)
                {
                    await heartbeatCancellation.CancelAsync();

                    if (!await readTask)
                    {
                        break;
                    }

                    while (subscription.Reader.TryRead(out var notificationEvent))
                    {
                        await WriteEventAsync(httpContext.Response, notificationEvent, cancellationToken);
                    }
                }
                else
                {
                    await WriteEventAsync(
                        httpContext.Response,
                        new NotificationSseEvent(
                            "heartbeat",
                            JsonSerializer.Serialize(new
                            {
                                sentAtUtc = timeProvider.GetUtcNow().UtcDateTime
                            })),
                        cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Browser disconnected or request was aborted.
        }
    }

    private static async Task WriteEventAsync(
        HttpResponse response,
        NotificationSseEvent notificationEvent,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(notificationEvent.EventId))
        {
            await response.WriteAsync($"id: {SanitizeLine(notificationEvent.EventId)}\n", cancellationToken);
        }

        await response.WriteAsync($"event: {SanitizeLine(notificationEvent.EventName)}\n", cancellationToken);

        foreach (var line in notificationEvent.Data.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            await response.WriteAsync($"data: {line}\n", cancellationToken);
        }

        await response.WriteAsync("\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }

    private static string SanitizeLine(string value)
    {
        return value.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
    }
}
