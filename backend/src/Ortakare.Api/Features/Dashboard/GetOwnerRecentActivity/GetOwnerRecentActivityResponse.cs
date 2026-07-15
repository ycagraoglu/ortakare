namespace Ortakare.Api.Features.Dashboard.GetOwnerRecentActivity;

public sealed record GetOwnerRecentActivityResponse(
    IReadOnlyList<OwnerRecentActivityItem> Items);

public sealed record OwnerRecentActivityItem(
    string ActivityType,
    Guid EventId,
    string EventTitle,
    Guid ParticipantId,
    string ParticipantDisplayName,
    Guid? PhotoId,
    string? ContentType,
    long? FileSizeBytes,
    string? ReadUrl,
    DateTime? ReadUrlExpiresAtUtc,
    bool? IsBlocked,
    DateTime CreatedAtUtc);
