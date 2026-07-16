using Ortakare.Api.Infrastructure.DomainEvents;

namespace Ortakare.Api.Features.Events.DomainEvents;

public sealed record EventClosedDomainEvent(
    Guid EventId,
    Guid OwnerUserId,
    DateTime OccurredAtUtc) : IDomainEvent;
