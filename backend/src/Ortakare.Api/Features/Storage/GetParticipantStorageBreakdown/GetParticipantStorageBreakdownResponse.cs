namespace Ortakare.Api.Features.Storage.GetParticipantStorageBreakdown;

public sealed record GetParticipantStorageBreakdownResponse(
    Guid EventId,
    string EventTitle,
    long TotalStorageBytes,
    int TotalPhotoCount,
    IReadOnlyList<ParticipantStorageBreakdownItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ParticipantStorageBreakdownItem(
    Guid ParticipantId,
    string DisplayName,
    bool IsBlocked,
    DateTime? BlockedAtUtc,
    DateTime JoinedAtUtc,
    int PhotoCount,
    long TotalStorageBytes,
    long AveragePhotoSizeBytes,
    DateTime? LastUploadAtUtc);
