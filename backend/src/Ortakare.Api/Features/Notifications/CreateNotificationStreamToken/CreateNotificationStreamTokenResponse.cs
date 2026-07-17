namespace Ortakare.Api.Features.Notifications.CreateNotificationStreamToken;

public sealed record CreateNotificationStreamTokenResponse(
    string Token,
    DateTime ExpiresAtUtc);
