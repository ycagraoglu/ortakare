namespace Ortakare.Api.Infrastructure.BackgroundJobs;

public sealed class GalleryExportCleanupOptions
{
    public const string SectionName = "GalleryExportCleanup";

    public bool Enabled { get; init; } = true;
    public int IntervalMinutes { get; init; } = 60;
    public int BatchSize { get; init; } = 100;
}
