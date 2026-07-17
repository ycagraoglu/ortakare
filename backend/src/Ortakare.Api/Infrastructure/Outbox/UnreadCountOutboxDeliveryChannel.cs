using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Notifications;
using Ortakare.Api.Infrastructure.Persistence;
using Ortakare.Api.Infrastructure.Realtime;

namespace Ortakare.Api.Infrastructure.Outbox;

public sealed class UnreadCountOutboxDeliveryChannel(
    OrtakareDbContext dbContext,
    IRealtimePublisher realtimePublisher,
    TimeProvider timeProvider,
    ILogger<UnreadCountOutboxDeliveryChannel> logger) : IOutboxDeliveryChannel
{
    private const string NotificationCreatedMessageType = "OwnerNotificationCreated";
    private const string RealtimeEventType = "UnreadCountChanged";

    public async Task DeliverAsync(
        string messageType,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(messageType, NotificationCreatedMessageType, StringComparison.Ordinal) &&
            !string.Equals(messageType, OwnerUnreadCountOutboxWriter.MessageType, StringComparison.Ordinal))
        {
            return;
        }

        var payload = JsonSerializer.Deserialize<OwnerPayload>(payloadJson)
            ?? throw new InvalidOperationException("Unread count outbox payload could not be deserialized.");

        if (payload.OwnerUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Unread count outbox payload contains an invalid owner identifier.");
        }

        var unreadCount = await dbContext.Notifications
            .AsNoTracking()
            .CountAsync(
                x => x.OwnerUserId == payload.OwnerUserId && x.ReadAtUtc == null,
                cancellationToken);

        var occurredAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var realtimePayloadJson = JsonSerializer.Serialize(new
        {
            unreadCount
        });

        await realtimePublisher.PublishAsync(
            payload.OwnerUserId,
            new RealtimeEvent(
                Type: RealtimeEventType,
                PayloadJson: realtimePayloadJson,
                EventId: null,
                OccurredAtUtc: occurredAtUtc),
            cancellationToken);

        logger.LogDebug(
            "Unread notification count published. OwnerUserId: {OwnerUserId}, UnreadCount: {UnreadCount}",
            payload.OwnerUserId,
            unreadCount);
    }

    private sealed record OwnerPayload(Guid OwnerUserId);
}
