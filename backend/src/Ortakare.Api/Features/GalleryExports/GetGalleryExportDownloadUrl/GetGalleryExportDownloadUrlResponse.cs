namespace Ortakare.Api.Features.GalleryExports.GetGalleryExportDownloadUrl;

public sealed record GetGalleryExportDownloadUrlResponse(
    Guid ExportId,
    string DownloadUrl,
    DateTime ExpiresAtUtc);
