namespace Ortakare.Api.Infrastructure.Outbox;

public sealed class LoggingOutboxDeliveryChannel(
    ILogger<LoggingOutboxDeliveryChannel> logger) : IOutboxDeliveryChannel
{
    public Task DeliverAsync(
        string messageType,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Outbox message delivered to logging channel. MessageType: {MessageType}, PayloadLength: {PayloadLength}",
            messageType,
            payloadJson.Length);

        return Task.CompletedTask;
    }
}
