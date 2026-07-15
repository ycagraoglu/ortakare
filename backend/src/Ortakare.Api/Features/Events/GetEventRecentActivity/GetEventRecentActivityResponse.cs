namespace Ortakare.Api.Features.Events.GetEventRecentActivity;

public sealed record GetEventRecentActivityResponse(
    IReadOnlyList<RecentParticipantItem> RecentParticipants,
    IReadOnlyList<RecentPhotoItem> RecentPhotos);

public sealed record RecentParticipantItem(
    Guid ParticipantId,
    string DisplayName,
    bool IsBlocked,
    DateTime? BlockedAtUtc,
    DateTime CreatedAtUtc);

public sealed record RecentPhotoItem(
    Guid PhotoId,
    Guid ParticipantId,
    string ParticipantDisplayName,
    string ContentType,
    long FileSizeBytes,
    DateTime CreatedAtUtc,
    string ReadUrl,
    DateTime ReadUrlExpiresAtUtc);
