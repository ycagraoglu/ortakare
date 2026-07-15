namespace Ortakare.Api.Features.GalleryExports.DeleteGalleryExport;

public sealed record DeleteGalleryExportResponse(
    Guid ExportId,
    GalleryExportStatus Status,
    bool StorageObjectDeleted);
