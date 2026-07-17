using System.Text.Json;

namespace Ortakare.Api.Infrastructure.Realtime;

public sealed record RealtimeEventEnvelope(
    Guid OwnerUserId,
    RealtimeEvent Event);

public interface IRealtimeEventFactory
{
    RealtimeEventEnvelope? Create(
        string messageType,
        string payloadJson);
}

public sealed class RealtimeEventFactory : IRealtimeEventFactory
{
    private const string OwnerNotificationCreatedMessageType = "OwnerNotificationCreated";
    private const string NotificationCreatedEventType = "NotificationCreated";

    public RealtimeEventEnvelope? Create(
        string messageType,
        string payloadJson)
    {
        if (!string.Equals(messageType, OwnerNotificationCreatedMessageType, StringComparison.Ordinal))
        {
            return null;
        }

        var payload = JsonSerializer.Deserialize<OwnerNotificationCreatedPayload>(payloadJson)
            ?? throw new InvalidOperationException("Owner notification outbox payload could not be deserialized.");

        if (payload.OwnerUserId == Guid.Empty || payload.NotificationId == Guid.Empty)
        {
            throw new InvalidOperationException("Owner notification outbox payload contains invalid identifiers.");
        }

        return new RealtimeEventEnvelope(
            payload.OwnerUserId,
            new RealtimeEvent(
                Type: NotificationCreatedEventType,
                PayloadJson: payloadJson,
                EventId: payload.NotificationId.ToString(),
                OccurredAtUtc: payload.OccurredAtUtc));
    }

    private sealed record OwnerNotificationCreatedPayload(
        Guid NotificationId,
        Guid OwnerUserId,
        DateTime? OccurredAtUtc);
}
