using Ortakare.Api.Features.Participants.DomainEvents;
using Ortakare.Api.Infrastructure.DomainEvents;

namespace Ortakare.Api.Features.EventAudit.Handlers;

public sealed class ParticipantBlockedAuditHandler(EventAuditWriter auditWriter)
    : IDomainEventHandler<ParticipantBlockedDomainEvent>
{
    public Task HandleAsync(
        ParticipantBlockedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        auditWriter.AddOwnerAction(
            domainEvent.EventId,
            domainEvent.OwnerUserId,
            "ParticipantBlocked",
            "Katılımcının etkinlik erişimi engellendi.",
            "Participant",
            domainEvent.ParticipantId);

        return Task.CompletedTask;
    }
}
