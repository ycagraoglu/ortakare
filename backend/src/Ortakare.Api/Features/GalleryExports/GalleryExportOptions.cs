namespace Ortakare.Api.Features.GalleryExports;

public sealed class GalleryExportOptions
{
    public const string SectionName = "GalleryExport";

    public int RetentionDays { get; init; } = 7;
}
