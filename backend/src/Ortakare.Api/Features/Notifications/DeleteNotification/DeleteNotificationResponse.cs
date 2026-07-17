namespace Ortakare.Api.Features.Notifications.DeleteNotification;

public sealed record DeleteNotificationResponse(
    Guid NotificationId,
    DateTime DeletedAtUtc);