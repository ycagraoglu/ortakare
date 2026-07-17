namespace Ortakare.Api.Features.Notifications.MarkAllNotificationsAsRead;

public sealed record MarkAllNotificationsAsReadResponse(
    int UpdatedCount,
    DateTime ReadAtUtc);
