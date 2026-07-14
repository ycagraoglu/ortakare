namespace Ortakare.Api.Features.Photos;

public sealed class EventGuestPhoto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid ParticipantId { get; set; }
    public Guid ClientUploadId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}