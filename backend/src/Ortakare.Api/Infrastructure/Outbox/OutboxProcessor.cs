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
        var lockId = Guid.CreateVersion7();
        var claimedIds = await ClaimBatchAsync(lockId, cancellationToken);

        if (claimedIds.Count == 0)
        {
            return 0;
        }

        foreach (var messageId in claimedIds)
        {
            await ProcessClaimedMessageAsync(messageId, lockId, cancellationToken);
        }

        return claimedIds.Count;
    }

    private async Task<IReadOnlyList<Guid>> ClaimBatchAsync(
        Guid lockId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var lockExpiredBefore = now.AddSeconds(-_options.LockTimeoutSeconds);

        var candidateIds = await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(x => x.ProcessedAtUtc == null &&
                        x.RetryCount < _options.MaxRetryCount &&
                        (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= now) &&
                        (x.LockedAtUtc == null || x.LockedAtUtc <= lockExpiredBefore))
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        var claimedIds = new List<Guid>(candidateIds.Count);

        foreach (var candidateId in candidateIds)
        {
            var affectedRows = await dbContext.OutboxMessages
                .Where(x => x.Id == candidateId &&
                            x.ProcessedAtUtc == null &&
                            x.RetryCount < _options.MaxRetryCount &&
                            (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= now) &&
                            (x.LockedAtUtc == null || x.LockedAtUtc <= lockExpiredBefore))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.LockId, lockId)
                        .SetProperty(x => x.LockedAtUtc, now),
                    cancellationToken);

            if (affectedRows == 1)
            {
                claimedIds.Add(candidateId);
            }
        }

        return claimedIds;
    }

    private async Task ProcessClaimedMessageAsync(
        Guid messageId,
        Guid lockId,
        CancellationToken cancellationToken)
    {
        var message = await dbContext.OutboxMessages
            .SingleOrDefaultAsync(
                x => x.Id == messageId &&
                     x.LockId == lockId &&
                     x.ProcessedAtUtc == null,
                cancellationToken);

        if (message is null)
        {
            return;
        }

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
        finally
        {
            message.LockId = null;
            message.LockedAtUtc = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    private int CalculateRetryDelaySeconds(int retryCount)
    {
        var multiplier = Math.Pow(2, Math.Max(0, retryCount - 1));
        var delay = _options.InitialRetryDelaySeconds * multiplier;
        return (int)Math.Min(delay, _options.MaxRetryDelaySeconds);
    }
}