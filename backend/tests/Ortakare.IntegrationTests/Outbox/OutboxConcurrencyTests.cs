using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ortakare.Api.Infrastructure.Observability;
using Ortakare.Api.Infrastructure.Outbox;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Outbox;

public sealed class OutboxConcurrencyTests
{
    [Fact]
    public async Task Concurrent_processors_deliver_the_same_message_only_once(
        CancellationToken cancellationToken)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"ortakare-outbox-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath};Cache=Shared;Default Timeout=10";

        try
        {
            var dbOptions = new DbContextOptionsBuilder<OrtakareDbContext>()
                .UseSqlite(connectionString)
                .Options;

            await using (var setupContext = new OrtakareDbContext(dbOptions))
            {
                await setupContext.Database.EnsureCreatedAsync(cancellationToken);
                setupContext.OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.CreateVersion7(),
                    Type = "test.notification",
                    PayloadJson = "{}",
                    OccurredAtUtc = DateTime.UtcNow
                });
                await setupContext.SaveChangesAsync(cancellationToken);
            }

            var channel = new CountingDeliveryChannel();
            var processorOptions = Options.Create(new OutboxProcessingOptions
            {
                BatchSize = 10,
                MaxRetryCount = 3,
                LockTimeoutSeconds = 30
            });

            await using var firstContext = new OrtakareDbContext(dbOptions);
            await using var secondContext = new OrtakareDbContext(dbOptions);

            var firstProcessor = CreateProcessor(firstContext, channel, processorOptions);
            var secondProcessor = CreateProcessor(secondContext, channel, processorOptions);

            await Task.WhenAll(
                firstProcessor.ProcessBatchAsync(cancellationToken),
                secondProcessor.ProcessBatchAsync(cancellationToken));

            await using var verificationContext = new OrtakareDbContext(dbOptions);
            var message = await verificationContext.OutboxMessages.SingleAsync(cancellationToken);

            Assert.Equal(1, channel.DeliveryCount);
            Assert.NotNull(message.ProcessedAtUtc);
            Assert.Null(message.LockId);
            Assert.Null(message.LockedAtUtc);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private static OutboxProcessor CreateProcessor(
        OrtakareDbContext dbContext,
        IOutboxDeliveryChannel channel,
        IOptions<OutboxProcessingOptions> options)
    {
        return new OutboxProcessor(
            dbContext,
            new OutboxDeliveryDispatcher([channel]),
            options,
            TimeProvider.System,
            new OrtakareTelemetry(),
            NullLogger<OutboxProcessor>.Instance);
    }

    private sealed class CountingDeliveryChannel : IOutboxDeliveryChannel
    {
        private int _deliveryCount;

        public int DeliveryCount => Volatile.Read(ref _deliveryCount);

        public async Task DeliverAsync(
            string messageType,
            string payloadJson,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _deliveryCount);
            await Task.Delay(50, cancellationToken);
        }
    }
}