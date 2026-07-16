namespace Ortakare.Api.Features.Notifications.MarkNotificationAsRead;

public sealed record MarkNotificationAsReadResponse(
    Guid NotificationId,
    bool IsRead,
    DateTime ReadAtUtc);
