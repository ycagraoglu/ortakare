namespace Ortakare.Api.Features.Storage.GetParticipantStorageDetail;

public sealed record GetParticipantStorageDetailResponse(
    Guid EventId,
    string EventTitle,
    Guid ParticipantId,
    string DisplayName,
    bool IsBlocked,
    DateTime? BlockedAtUtc,
    DateTime JoinedAtUtc,
    int PhotoCount,
    long TotalStorageBytes,
    long AveragePhotoSizeBytes,
    DateTime? FirstUploadAtUtc,
    DateTime? LastUploadAtUtc,
    IReadOnlyList<ParticipantStorageTrendDay> Days,
    IReadOnlyList<ParticipantRecentPhotoItem> RecentPhotos);

public sealed record ParticipantStorageTrendDay(
    DateOnly Date,
    long AddedBytes,
    int PhotoCount);

public sealed record ParticipantRecentPhotoItem(
    Guid PhotoId,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateTime CreatedAtUtc,
    string ReadUrl,
    DateTime ReadUrlExpiresAtUtc);
