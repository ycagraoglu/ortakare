namespace Ortakare.Api.Features.GalleryExports.RetryFailedGalleryExport;

public sealed record RetryFailedGalleryExportResponse(
    Guid ExportId,
    GalleryExportStatus Status,
    int PhotoCount,
    DateTime CreatedAtUtc);
