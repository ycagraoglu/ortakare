using Ortakare.Api.Features.Events.DomainEvents;
using Ortakare.Api.Infrastructure.DomainEvents;

namespace Ortakare.Api.Features.EventAudit.Handlers;

public sealed class EventClosedAuditHandler(EventAuditWriter auditWriter)
    : IDomainEventHandler<EventClosedDomainEvent>
{
    public Task HandleAsync(
        EventClosedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        auditWriter.AddOwnerAction(
            domainEvent.EventId,
            domainEvent.OwnerUserId,
            "EventClosed",
            "Etkinlik yeni yüklemelere kapatıldı.");

        return Task.CompletedTask;
    }
}
