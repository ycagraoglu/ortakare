using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Infrastructure.Outbox;

public sealed class OutboxProcessor(
    OrtakareDbContext dbContext,
    OutboxDeliveryDispatcher deliveryDispatcher,
    IOptions<OutboxProcessingOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor> logger)
{
    private readonly OutboxProcessingOptions _options = options.Value;

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var messages = await dbContext.OutboxMessages
            .Where(x => x.ProcessedAtUtc == null &&
                        x.RetryCount < _options.MaxRetryCount &&
                        (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= now))
            .OrderBy(x => x.OccurredAtUtc)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await deliveryDispatcher.DispatchAsync(message, cancellationToken);
                message.ProcessedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
                message.NextAttemptAtUtc = null;
                message.LastError = null;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                message.RetryCount++;
                message.LastError = exception.Message.Length <= 1000
                    ? exception.Message
                    : exception.Message[..1000];
                message.NextAttemptAtUtc = timeProvider.GetUtcNow().UtcDateTime
                    .AddSeconds(CalculateRetryDelaySeconds(message.RetryCount));

                logger.LogWarning(
                    exception,
                    "Outbox message delivery failed. MessageId: {MessageId}, RetryCount: {RetryCount}",
                    message.Id,
                    message.RetryCount);
            }
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return messages.Count;
    }

    private int CalculateRetryDelaySeconds(int retryCount)
    {
        var multiplier = Math.Pow(2, Math.Max(0, retryCount - 1));
        var delay = _options.InitialRetryDelaySeconds * multiplier;
        return (int)Math.Min(delay, _options.MaxRetryDelaySeconds);
    }
}
