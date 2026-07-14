namespace Ortakare.Api.Features.GalleryExports.CreateGalleryExport;

public sealed record CreateGalleryExportResponse(
    Guid ExportId,
    GalleryExportStatus Status,
    int PhotoCount,
    DateTime CreatedAtUtc);
