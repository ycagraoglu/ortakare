namespace Ortakare.Api.Features.Notifications;

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? EventId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = NotificationSeverities.Info;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string? DataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}

public static class NotificationSeverities
{
    public const string Info = "Info";
    public const string Success = "Success";
    public const string Warning = "Warning";
    public const string Error = "Error";
}