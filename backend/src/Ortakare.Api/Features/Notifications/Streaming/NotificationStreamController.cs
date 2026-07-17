using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Ortakare.Api.Features.Notifications.Streaming;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationStreamController(
    INotificationStreamTokenService streamTokenService,
    IOptions<NotificationStreamOptions> options,
    TimeProvider timeProvider) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("stream")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task Stream(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var consumedToken = streamTokenService.Consume(token);

        if (consumedToken is null)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        Response.Headers.CacheControl = "no-cache, no-store";
        Response.Headers.Pragma = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        var result = TypedResults.ServerSentEvents(
            CreateEventStream(
                options.Value.HeartbeatSeconds,
                cancellationToken));

        await result.ExecuteAsync(HttpContext);
    }

    private async IAsyncEnumerable<SseItem<NotificationStreamEvent>> CreateEventStream(
        int heartbeatSeconds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new SseItem<NotificationStreamEvent>(
            new NotificationStreamEvent(timeProvider.GetUtcNow().UtcDateTime),
            "connected");

        using var heartbeatTimer = new PeriodicTimer(
            TimeSpan.FromSeconds(heartbeatSeconds),
            timeProvider);

        while (!cancellationToken.IsCancellationRequested)
        {
            bool hasNextTick;

            try
            {
                hasNextTick = await heartbeatTimer.WaitForNextTickAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            if (!hasNextTick)
            {
                yield break;
            }

            yield return new SseItem<NotificationStreamEvent>(
                new NotificationStreamEvent(timeProvider.GetUtcNow().UtcDateTime),
                "heartbeat");
        }
    }
}

public sealed record NotificationStreamEvent(DateTime SentAtUtc);