namespace Ortakare.Api.Features.Notifications.GetMyNotifications;

public sealed record GetMyNotificationsResponse(
    IReadOnlyList<MyNotificationItem> Items,
    string? NextCursor,
    bool HasMore);

public sealed record MyNotificationItem(
    Guid NotificationId,
    Guid? EventId,
    string? EventTitle,
    string Type,
    string Severity,
    string Title,
    string Message,
    string? ActionUrl,
    string? DataJson,
    bool IsRead,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);