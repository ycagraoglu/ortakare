using Ortakare.Api.Features.Participants.DomainEvents;
using Ortakare.Api.Infrastructure.DomainEvents;

namespace Ortakare.Api.Features.EventAudit.Handlers;

public sealed class ParticipantUnblockedAuditHandler(EventAuditWriter auditWriter)
    : IDomainEventHandler<ParticipantUnblockedDomainEvent>
{
    public Task HandleAsync(
        ParticipantUnblockedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        auditWriter.AddOwnerAction(
            domainEvent.EventId,
            domainEvent.OwnerUserId,
            "ParticipantUnblocked",
            "Katılımcının etkinlik erişim engeli kaldırıldı.",
            "Participant",
            domainEvent.ParticipantId);

        return Task.CompletedTask;
    }
}
