namespace Ortakare.Api.Features.EventAudit;

public sealed class EventAuditLog
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActorType { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public string? TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
