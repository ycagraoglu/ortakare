namespace Ortakare.Api.Features.Photos.GetEventPhotos;

public sealed record GetEventPhotosResponse(
    IReadOnlyList<GetEventPhotosItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record GetEventPhotosItem(
    Guid Id,
    string ParticipantDisplayName,
    string ContentType,
    long FileSizeBytes,
    DateTime CreatedAtUtc,
    string SignedUrl,
    DateTime SignedUrlExpiresAtUtc);