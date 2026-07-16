using Ortakare.Api.Features.Participants.DomainEvents;
using Ortakare.Api.Infrastructure.DomainEvents;

namespace Ortakare.Api.Features.Notifications.Handlers;

public sealed class ParticipantJoinedNotificationHandler(NotificationOutboxWriter writer)
    : IDomainEventHandler<ParticipantJoinedDomainEvent>
{
    public Task HandleAsync(
        ParticipantJoinedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        writer.AddOwnerNotification(
            domainEvent.OwnerUserId,
            domainEvent.EventId,
            "ParticipantJoined",
            "Yeni katılımcı",
            "Etkinliğinize yeni bir katılımcı katıldı.",
            domainEvent.OccurredAtUtc,
            new { domainEvent.ParticipantId },
            NotificationSeverities.Info,
            $"/events/{domainEvent.EventId}/participants");

        return Task.CompletedTask;
    }
}