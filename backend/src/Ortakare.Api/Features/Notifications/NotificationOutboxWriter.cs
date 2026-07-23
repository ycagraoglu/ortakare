using System.Text.Json;
using Ortakare.Api.Infrastructure.Outbox;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Notifications;

public sealed class NotificationOutboxWriter(OrtakareDbContext dbContext)
{
    public void AddOwnerNotification(
        Guid ownerUserId,
        Guid? eventId,
        string type,
        string title,
        string message,
        DateTime occurredAtUtc,
        object? data = null,
        string severity = NotificationSeverities.Info,
        string? actionUrl = null)
    {
        if (actionUrl is not null && (!actionUrl.StartsWith("/", StringComparison.Ordinal) || actionUrl.StartsWith("//", StringComparison.Ordinal)))
        {
            throw new ArgumentException("Notification action URL must be a local absolute application route.", nameof(actionUrl));
        }

        var dataJson = data is null ? null : JsonSerializer.Serialize(data);
        var notificationId = Guid.CreateVersion7();

        dbContext.Notifications.Add(new Notification
        {
            Id = notificationId,
            OwnerUserId = ownerUserId,
            EventId = eventId,
            Type = type,
            Severity = severity,
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            DataJson = dataJson,
            CreatedAtUtc = occurredAtUtc
        });

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            Type = "OwnerNotificationCreated",
            PayloadJson = JsonSerializer.Serialize(new
            {
                NotificationId = notificationId,
                OwnerUserId = ownerUserId,
                EventId = eventId,
                NotificationType = type,
                Severity = severity,
                Title = title,
                Message = message,
                ActionUrl = actionUrl,
                DataJson = dataJson,
                OccurredAtUtc = occurredAtUtc
            }),
            OccurredAtUtc = occurredAtUtc
        });
    }
}