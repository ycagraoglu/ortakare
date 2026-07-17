using Ortakare.Api.Infrastructure.Realtime;

namespace Ortakare.Api.Infrastructure.Outbox;

public sealed class SseOutboxDeliveryChannel(
    IRealtimeEventFactory realtimeEventFactory,
    IRealtimePublisher realtimePublisher,
    ILogger<SseOutboxDeliveryChannel> logger) : IOutboxDeliveryChannel
{
    public async Task DeliverAsync(
        string messageType,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var envelope = realtimeEventFactory.Create(messageType, payloadJson);

        if (envelope is null)
        {
            return;
        }

        await realtimePublisher.PublishAsync(
            envelope.OwnerUserId,
            envelope.Event,
            cancellationToken);

        logger.LogDebug(
            "Outbox message published as realtime event. MessageType: {MessageType}, EventType: {EventType}, OwnerUserId: {OwnerUserId}",
            messageType,
            envelope.Event.Type,
            envelope.OwnerUserId);
    }
}
