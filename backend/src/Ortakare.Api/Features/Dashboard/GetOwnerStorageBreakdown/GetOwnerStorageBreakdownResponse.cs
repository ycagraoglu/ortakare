namespace Ortakare.Api.Features.Dashboard.GetOwnerStorageBreakdown;

public sealed record GetOwnerStorageBreakdownResponse(
    OwnerStorageOverview Overview,
    IReadOnlyList<OwnerStorageEventItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record OwnerStorageOverview(
    int TotalEventCount,
    int TotalPhotoCount,
    long TotalStorageBytes,
    long AverageStoragePerEventBytes,
    long AveragePhotoSizeBytes,
    OwnerStorageHighlight? LargestStorageEvent,
    OwnerStorageHighlight? MostPhotosEvent,
    DateTime? LastUploadAtUtc);

public sealed record OwnerStorageHighlight(
    Guid EventId,
    string Title,
    int PhotoCount,
    long TotalStorageBytes);

public sealed record OwnerStorageEventItem(
    Guid EventId,
    string Title,
    DateTime EventDateUtc,
    bool UploadsEnabled,
    int PhotoCount,
    long TotalStorageBytes,
    long AveragePhotoSizeBytes,
    DateTime? LastUploadAtUtc);
