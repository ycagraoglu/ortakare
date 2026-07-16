namespace Ortakare.Api.Infrastructure.Outbox;

public sealed class OutboxDeliveryDispatcher(
    IEnumerable<IOutboxDeliveryChannel> deliveryChannels)
{
    public async Task DispatchAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        foreach (var channel in deliveryChannels)
        {
            await channel.DeliverAsync(
                message.Type,
                message.PayloadJson,
                cancellationToken);
        }
    }
}
