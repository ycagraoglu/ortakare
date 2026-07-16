namespace Ortakare.Api.Infrastructure.Outbox;

public interface IOutboxDeliveryChannel
{
    Task DeliverAsync(
        string messageType,
        string payloadJson,
        CancellationToken cancellationToken);
}
