using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Notifications;
using Ortakare.Api.Features.Notifications.DeleteNotification;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Notifications;

public sealed class DeleteNotificationHandlerTests
{
    [Fact]
    public async Task HandleAsync_SoftDeletesOwnersNotificationAndHidesItFromDefaultQueries()
    {
        var ownerUserId = Guid.CreateVersion7();
        var notificationId = Guid.CreateVersion7();
        var deletedAtUtc = new DateTimeOffset(2026, 7, 17, 9, 30, 0, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        dbContext.Notifications.Add(CreateNotification(notificationId, ownerUserId));
        await dbContext.SaveChangesAsync();

        var handler = new DeleteNotificationHandler(
            dbContext,
            new TestCurrentUser(ownerUserId),
            new FixedTimeProvider(deletedAtUtc));

        var result = await handler.HandleAsync(notificationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(notificationId, result.Data!.NotificationId);
        Assert.Equal(deletedAtUtc.UtcDateTime, result.Data.DeletedAtUtc);
        Assert.Empty(await dbContext.Notifications.AsNoTracking().ToListAsync());

        var deletedNotification = await dbContext.Notifications
            .IgnoreQueryFilters()
            .SingleAsync();
        Assert.Equal(deletedAtUtc.UtcDateTime, deletedNotification.DeletedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_IsIdempotentAndPreservesFirstDeletionTime()
    {
        var ownerUserId = Guid.CreateVersion7();
        var notificationId = Guid.CreateVersion7();
        var firstDeletedAtUtc = new DateTime(2026, 7, 17, 9, 0, 0, DateTimeKind.Utc);
        await using var dbContext = CreateDbContext();
        dbContext.Notifications.Add(CreateNotification(
            notificationId,
            ownerUserId,
            firstDeletedAtUtc));
        await dbContext.SaveChangesAsync();

        var handler = new DeleteNotificationHandler(
            dbContext,
            new TestCurrentUser(ownerUserId),
            new FixedTimeProvider(new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero)));

        var result = await handler.HandleAsync(notificationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(firstDeletedAtUtc, result.Data!.DeletedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFoundForAnotherOwnersNotification()
    {
        var notificationId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Notifications.Add(CreateNotification(
            notificationId,
            Guid.CreateVersion7()));
        await dbContext.SaveChangesAsync();

        var handler = new DeleteNotificationHandler(
            dbContext,
            new TestCurrentUser(Guid.CreateVersion7()),
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        var result = await handler.HandleAsync(notificationId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Null((await dbContext.Notifications.IgnoreQueryFilters().SingleAsync()).DeletedAtUtc);
    }

    private static Notification CreateNotification(
        Guid notificationId,
        Guid ownerUserId,
        DateTime? deletedAtUtc = null) => new()
    {
        Id = notificationId,
        OwnerUserId = ownerUserId,
        Type = "Test",
        Severity = NotificationSeverities.Info,
        Title = "Test notification",
        Message = "Test message",
        CreatedAtUtc = new DateTime(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc),
        DeletedAtUtc = deletedAtUtc
    };

    private static OrtakareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrtakareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrtakareDbContext(options);
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}