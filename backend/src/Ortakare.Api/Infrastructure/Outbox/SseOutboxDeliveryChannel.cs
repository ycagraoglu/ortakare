using System.Text.Json;
using Ortakare.Api.Features.Notifications.Streaming;

namespace Ortakare.Api.Infrastructure.Outbox;

public sealed class SseOutboxDeliveryChannel(
    INotificationRealtimePublisher realtimePublisher,
    ILogger<SseOutboxDeliveryChannel> logger) : IOutboxDeliveryChannel
{
    private const string OwnerNotificationCreatedMessageType = "OwnerNotificationCreated";

    public async Task DeliverAsync(
        string messageType,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(messageType, OwnerNotificationCreatedMessageType, StringComparison.Ordinal))
        {
            return;
        }

        var payload = JsonSerializer.Deserialize<OwnerNotificationCreatedPayload>(payloadJson)
            ?? throw new InvalidOperationException("Owner notification outbox payload could not be deserialized.");

        if (payload.OwnerUserId == Guid.Empty || payload.NotificationId == Guid.Empty)
        {
            throw new InvalidOperationException("Owner notification outbox payload contains invalid identifiers.");
        }

        await realtimePublisher.PublishAsync(
            payload.OwnerUserId,
            new NotificationSseEvent(
                EventName: "notification-created",
                Data: payloadJson,
                EventId: payload.NotificationId.ToString()),
            cancellationToken);

        logger.LogDebug(
            "Owner notification published to SSE connections. NotificationId: {NotificationId}, OwnerUserId: {OwnerUserId}",
            payload.NotificationId,
            payload.OwnerUserId);
    }

    private sealed record OwnerNotificationCreatedPayload(
        Guid NotificationId,
        Guid OwnerUserId);
}
