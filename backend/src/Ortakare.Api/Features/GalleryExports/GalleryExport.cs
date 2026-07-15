namespace Ortakare.Api.Features.GalleryExports;

public sealed class GalleryExport
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public GalleryExportStatus Status { get; set; }
    public int PhotoCount { get; set; }
    public string? StorageKey { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? FailedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
}

public enum GalleryExportStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}
