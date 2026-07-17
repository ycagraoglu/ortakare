using System.Text.Json;
using Ortakare.Api.Infrastructure.Outbox;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Notifications;

public sealed class OwnerUnreadCountOutboxWriter(
    OrtakareDbContext dbContext,
    TimeProvider timeProvider)
{
    public const string MessageType = "OwnerUnreadCountRefreshRequested";

    public void Add(Guid ownerUserId)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new ArgumentException("Owner user id cannot be empty.", nameof(ownerUserId));
        }

        var occurredAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            Type = MessageType,
            PayloadJson = JsonSerializer.Serialize(new
            {
                OwnerUserId = ownerUserId,
                OccurredAtUtc = occurredAtUtc
            }),
            OccurredAtUtc = occurredAtUtc
        });
    }
}
