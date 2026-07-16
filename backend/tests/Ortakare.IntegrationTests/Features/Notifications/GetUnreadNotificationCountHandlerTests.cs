using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Notifications;
using Ortakare.Api.Features.Notifications.GetUnreadNotificationCount;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Notifications;

public sealed class GetUnreadNotificationCountHandlerTests
{
    [Fact]
    public async Task HandleAsync_CountsOnlyCurrentOwnersUnreadNotifications()
    {
        var ownerUserId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Notifications.AddRange(
            CreateNotification(ownerUserId, readAtUtc: null),
            CreateNotification(ownerUserId, readAtUtc: null),
            CreateNotification(ownerUserId, new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc)),
            CreateNotification(Guid.CreateVersion7(), readAtUtc: null));
        await dbContext.SaveChangesAsync();

        var handler = new GetUnreadNotificationCountHandler(
            dbContext,
            new TestCurrentUser(ownerUserId));

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
    }

    [Fact]
    public void NotificationWriter_RejectsExternalActionUrl()
    {
        using var dbContext = CreateDbContext();
        var writer = new NotificationOutboxWriter(dbContext);

        Assert.Throws<ArgumentException>(() => writer.AddOwnerNotification(
            Guid.CreateVersion7(),
            null,
            "Test",
            "Test",
            "Test",
            new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc),
            actionUrl: "https://example.com"));
    }

    private static OrtakareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrtakareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrtakareDbContext(options);
    }

    private static Notification CreateNotification(Guid ownerUserId, DateTime? readAtUtc) => new()
    {
        Id = Guid.CreateVersion7(),
        OwnerUserId = ownerUserId,
        Type = "Test",
        Severity = NotificationSeverities.Info,
        Title = "Test",
        Message = "Test",
        CreatedAtUtc = new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc),
        ReadAtUtc = readAtUtc
    };

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }
}