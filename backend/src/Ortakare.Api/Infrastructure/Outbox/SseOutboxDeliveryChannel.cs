using System.Text.Json;
using Ortakare.Api.Infrastructure.Realtime;

namespace Ortakare.Api.Infrastructure.Outbox;

public sealed class SseOutboxDeliveryChannel(
    IRealtimePublisher realtimePublisher,
    ILogger<SseOutboxDeliveryChannel> logger) : IOutboxDeliveryChannel
{
    private const string OwnerNotificationCreatedMessageType = "OwnerNotificationCreated";
    private const string NotificationCreatedEventType = "NotificationCreated";

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
            new RealtimeEvent(
                Type: NotificationCreatedEventType,
                PayloadJson: payloadJson,
                EventId: payload.NotificationId.ToString(),
                OccurredAtUtc: payload.OccurredAtUtc),
            cancellationToken);

        logger.LogDebug(
            "Owner notification published as realtime event. NotificationId: {NotificationId}, OwnerUserId: {OwnerUserId}",
            payload.NotificationId,
            payload.OwnerUserId);
    }

    private sealed record OwnerNotificationCreatedPayload(
        Guid NotificationId,
        Guid OwnerUserId,
        DateTime? OccurredAtUtc);
}
