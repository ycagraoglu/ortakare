namespace Ortakare.Api.Features.GalleryExports.GetEventExports;

public sealed record GetEventExportsResponse(
    IReadOnlyList<GetEventExportsItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record GetEventExportsItem(
    Guid ExportId,
    GalleryExportStatus Status,
    int PhotoCount,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? ExpiresAtUtc,
    bool IsExpired,
    DateTime? FailedAtUtc);
