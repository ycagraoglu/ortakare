using Ortakare.Api.Features.Events.DomainEvents;
using Ortakare.Api.Infrastructure.DomainEvents;

namespace Ortakare.Api.Features.EventAudit.Handlers;

public sealed class EventReopenedAuditHandler(EventAuditWriter auditWriter)
    : IDomainEventHandler<EventReopenedDomainEvent>
{
    public Task HandleAsync(
        EventReopenedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        auditWriter.AddOwnerAction(
            domainEvent.EventId,
            domainEvent.OwnerUserId,
            "EventReopened",
            "Etkinlik yeni yüklemelere yeniden açıldı.");

        return Task.CompletedTask;
    }
}
