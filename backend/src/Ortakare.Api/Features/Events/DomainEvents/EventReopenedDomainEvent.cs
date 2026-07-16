using Ortakare.Api.Infrastructure.DomainEvents;

namespace Ortakare.Api.Features.Events.DomainEvents;

public sealed record EventReopenedDomainEvent(
    Guid EventId,
    Guid OwnerUserId,
    DateTime OccurredAtUtc) : IDomainEvent;
