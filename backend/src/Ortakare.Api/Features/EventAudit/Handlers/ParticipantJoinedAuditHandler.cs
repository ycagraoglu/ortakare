using Ortakare.Api.Features.Participants.DomainEvents;
using Ortakare.Api.Infrastructure.DomainEvents;

namespace Ortakare.Api.Features.EventAudit.Handlers;

public sealed class ParticipantJoinedAuditHandler(EventAuditWriter auditWriter)
    : IDomainEventHandler<ParticipantJoinedDomainEvent>
{
    public Task HandleAsync(
        ParticipantJoinedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        auditWriter.AddOwnerAction(
            domainEvent.EventId,
            domainEvent.OwnerUserId,
            "ParticipantJoined",
            "Yeni bir katılımcı etkinliğe katıldı.",
            "Participant",
            domainEvent.ParticipantId);

        return Task.CompletedTask;
    }
}
