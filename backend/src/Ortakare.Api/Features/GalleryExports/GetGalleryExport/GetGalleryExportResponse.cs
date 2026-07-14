namespace Ortakare.Api.Features.GalleryExports.GetGalleryExport;

public sealed record GetGalleryExportResponse(
    Guid ExportId,
    GalleryExportStatus Status,
    int PhotoCount,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? FailedAtUtc,
    string? DownloadUrl,
    DateTime? DownloadUrlExpiresAtUtc);
