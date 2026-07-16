namespace Ortakare.Api.Features.EventAudit.GetEventAuditLogs;

public sealed record GetEventAuditLogsResponse(
    IReadOnlyList<EventAuditLogItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record EventAuditLogItem(
    Guid Id,
    string Action,
    string ActorType,
    Guid? ActorId,
    string? TargetType,
    Guid? TargetId,
    string Description,
    string? MetadataJson,
    DateTime CreatedAtUtc);
