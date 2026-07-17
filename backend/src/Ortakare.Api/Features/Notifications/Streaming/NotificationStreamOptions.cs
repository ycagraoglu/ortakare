namespace Ortakare.Api.Features.Notifications.Streaming;

public sealed class NotificationStreamOptions
{
    public const string SectionName = "NotificationStream";

    public int TokenLifetimeSeconds { get; init; } = 60;
}
