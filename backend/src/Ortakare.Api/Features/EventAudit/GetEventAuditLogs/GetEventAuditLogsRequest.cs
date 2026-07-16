namespace Ortakare.Api.Features.EventAudit.GetEventAuditLogs;

public sealed class GetEventAuditLogsRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
