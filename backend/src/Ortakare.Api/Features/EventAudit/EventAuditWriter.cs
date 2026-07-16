using System.Text.Json;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.EventAudit;

public sealed class EventAuditWriter(
    OrtakareDbContext dbContext,
    TimeProvider timeProvider)
{
    public void AddOwnerAction(
        Guid eventId,
        Guid ownerUserId,
        string action,
        string description,
        string? targetType = null,
        Guid? targetId = null,
        object? metadata = null)
    {
        dbContext.EventAuditLogs.Add(new EventAuditLog
        {
            Id = Guid.CreateVersion7(),
            EventId = eventId,
            OwnerUserId = ownerUserId,
            Action = action,
            ActorType = "Owner",
            ActorId = ownerUserId,
            TargetType = targetType,
            TargetId = targetId,
            Description = description,
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata),
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        });
    }
}
