namespace Ortakare.Api.Features.GalleryExports.CancelPendingGalleryExport;

public sealed record CancelPendingGalleryExportResponse(
    Guid ExportId,
    GalleryExportStatus Status,
    DateTime CancelledAtUtc);
