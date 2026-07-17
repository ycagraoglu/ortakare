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
    public IResult Stream([FromQuery] string token)
    {
        var consumedToken = streamTokenService.Consume(token);

        if (consumedToken is null)
        {
            return Results.Unauthorized();
        }

        Response.Headers.CacheControl = "no-cache, no-store";
        Response.Headers.Pragma = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        return TypedResults.ServerSentEvents(
            CreateEventStream(
                consumedToken.OwnerUserId,
                options.Value.HeartbeatSeconds,
                HttpContext.RequestAborted));
    }

    private async IAsyncEnumerable<SseItem<NotificationStreamEvent>> CreateEventStream(
        Guid ownerUserId,
        int heartbeatSeconds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new SseItem<NotificationStreamEvent>(
            new NotificationStreamEvent(
                ownerUserId,
                timeProvider.GetUtcNow().UtcDateTime),
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
                new NotificationStreamEvent(
                    ownerUserId,
                    timeProvider.GetUtcNow().UtcDateTime),
                "heartbeat");
        }
    }
}

public sealed record NotificationStreamEvent(
    Guid OwnerUserId,
    DateTime SentAtUtc);