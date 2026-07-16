namespace Ortakare.Api.Infrastructure.DomainEvents;

public interface IDomainEventDispatcher
{
    Task PublishAsync<TDomainEvent>(
        TDomainEvent domainEvent,
        CancellationToken cancellationToken)
        where TDomainEvent : IDomainEvent;
}
