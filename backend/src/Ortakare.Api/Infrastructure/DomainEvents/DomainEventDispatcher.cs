namespace Ortakare.Api.Infrastructure.DomainEvents;

public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task PublishAsync<TDomainEvent>(
        TDomainEvent domainEvent,
        CancellationToken cancellationToken)
        where TDomainEvent : IDomainEvent
    {
        var handlers = serviceProvider.GetServices<IDomainEventHandler<TDomainEvent>>();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(domainEvent, cancellationToken);
        }
    }
}
