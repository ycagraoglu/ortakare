using Ortakare.Api.Infrastructure.DomainEvents;

namespace Ortakare.Api.Features.Participants.DomainEvents;

public sealed record ParticipantJoinedDomainEvent(
    Guid EventId,
    Guid OwnerUserId,
    Guid ParticipantId,
    DateTime OccurredAtUtc) : IDomainEvent;
