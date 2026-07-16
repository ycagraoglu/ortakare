using FluentValidation;

namespace Ortakare.Api.Features.EventAudit.GetEventAuditLogs;

public sealed class GetEventAuditLogsValidator : AbstractValidator<GetEventAuditLogsRequest>
{
    public GetEventAuditLogsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
