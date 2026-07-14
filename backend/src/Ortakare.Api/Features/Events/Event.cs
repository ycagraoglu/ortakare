namespace Ortakare.Api.Features.Events;

public sealed class Event
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime EventDateUtc { get; set; }
    public string GalleryToken { get; set; } = string.Empty;
    public bool UploadsEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}