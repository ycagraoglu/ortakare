using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Notifications;
using Ortakare.Api.Features.Notifications.Handlers;
using Ortakare.Api.Features.Participants.DomainEvents;
using Ortakare.Api.Features.Photos.DomainEvents;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Notifications;

public sealed class NotificationOutboxHandlerTests
{
    [Fact]
    public async Task ParticipantJoinedHandler_AddsNotificationAndOutboxMessage()
    {
        await using var dbContext = CreateDbContext();
        var writer = new NotificationOutboxWriter(dbContext);
        var handler = new ParticipantJoinedNotificationHandler(writer);
        var domainEvent = new ParticipantJoinedDomainEvent(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc));

        await handler.HandleAsync(domainEvent, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        var notification = await dbContext.Notifications.SingleAsync();
        var outboxMessage = await dbContext.OutboxMessages.SingleAsync();

        Assert.Equal(domainEvent.OwnerUserId, notification.OwnerUserId);
        Assert.Equal(domainEvent.EventId, notification.EventId);
        Assert.Equal("ParticipantJoined", notification.Type);
        Assert.Null(notification.ReadAtUtc);
        Assert.Equal("OwnerNotificationCreated", outboxMessage.Type);
        Assert.Null(outboxMessage.ProcessedAtUtc);
        Assert.Equal(0, outboxMessage.RetryCount);
    }

    [Fact]
    public async Task PhotoUploadedHandler_AddsFileMetadataWithoutStorageKey()
    {
        await using var dbContext = CreateDbContext();
        var writer = new NotificationOutboxWriter(dbContext);
        var handler = new PhotoUploadedNotificationHandler(writer);
        var domainEvent = new PhotoUploadedDomainEvent(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            4_200_000,
            new DateTime(2026, 7, 16, 10, 5, 0, DateTimeKind.Utc));

        await handler.HandleAsync(domainEvent, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        var notification = await dbContext.Notifications.SingleAsync();
        var outboxMessage = await dbContext.OutboxMessages.SingleAsync();

        Assert.Equal("PhotoUploaded", notification.Type);
        Assert.Contains(domainEvent.PhotoId.ToString(), notification.DataJson);
        Assert.Contains(domainEvent.FileSizeBytes.ToString(), notification.DataJson);
        Assert.DoesNotContain("StorageKey", notification.DataJson);
        Assert.DoesNotContain("StorageKey", outboxMessage.PayloadJson);
    }

    private static OrtakareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrtakareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrtakareDbContext(options);
    }
}
