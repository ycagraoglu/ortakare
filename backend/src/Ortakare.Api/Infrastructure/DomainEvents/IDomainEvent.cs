namespace Ortakare.Api.Infrastructure.DomainEvents;

public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}
