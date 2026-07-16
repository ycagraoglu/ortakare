using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Notifications;
using Ortakare.Api.Features.Notifications.MarkNotificationAsRead;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Notifications;

public sealed class MarkNotificationAsReadHandlerTests
{
    [Fact]
    public async Task HandleAsync_MarksOwnedNotificationAsRead()
    {
        var ownerUserId = Guid.CreateVersion7();
        var notificationId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 7, 16, 16, 0, 0, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        dbContext.Notifications.Add(CreateNotification(notificationId, ownerUserId));
        await dbContext.SaveChangesAsync();

        var handler = new MarkNotificationAsReadHandler(
            dbContext,
            new TestCurrentUser(ownerUserId),
            new TestTimeProvider(now));

        var result = await handler.HandleAsync(notificationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsRead);
        Assert.Equal(now.UtcDateTime, result.Data.ReadAtUtc);
        Assert.Equal(now.UtcDateTime, (await dbContext.Notifications.SingleAsync()).ReadAtUtc);
    }

    [Fact]
    public async Task HandleAsync_PreservesFirstReadTimeWhenCalledAgain()
    {
        var ownerUserId = Guid.CreateVersion7();
        var notificationId = Guid.CreateVersion7();
        var firstReadAt = new DateTime(2026, 7, 16, 15, 0, 0, DateTimeKind.Utc);
        await using var dbContext = CreateDbContext();
        var notification = CreateNotification(notificationId, ownerUserId);
        notification.ReadAtUtc = firstReadAt;
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();

        var handler = new MarkNotificationAsReadHandler(
            dbContext,
            new TestCurrentUser(ownerUserId),
            new TestTimeProvider(new DateTimeOffset(2026, 7, 16, 17, 0, 0, TimeSpan.Zero)));

        var result = await handler.HandleAsync(notificationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(firstReadAt, result.Data!.ReadAtUtc);
        Assert.Equal(firstReadAt, (await dbContext.Notifications.SingleAsync()).ReadAtUtc);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFoundForAnotherOwnersNotification()
    {
        var notificationId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Notifications.Add(CreateNotification(notificationId, Guid.CreateVersion7()));
        await dbContext.SaveChangesAsync();

        var handler = new MarkNotificationAsReadHandler(
            dbContext,
            new TestCurrentUser(Guid.CreateVersion7()),
            new TestTimeProvider(DateTimeOffset.UtcNow));

        var result = await handler.HandleAsync(notificationId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Null((await dbContext.Notifications.SingleAsync()).ReadAtUtc);
    }

    private static Notification CreateNotification(Guid notificationId, Guid ownerUserId) => new()
    {
        Id = notificationId,
        OwnerUserId = ownerUserId,
        Type = "Test",
        Severity = "Info",
        Title = "Test notification",
        Message = "Test message",
        CreatedAtUtc = new DateTime(2026, 7, 16, 14, 0, 0, DateTimeKind.Utc)
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

    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
