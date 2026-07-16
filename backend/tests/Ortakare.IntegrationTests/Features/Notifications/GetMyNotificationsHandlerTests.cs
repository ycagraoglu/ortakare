using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Notifications;
using Ortakare.Api.Features.Notifications.GetMyNotifications;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Notifications;

public sealed class GetMyNotificationsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsOwnerNotificationsNewestFirstWithCursor()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.Notifications.AddRange(
            CreateNotification(ownerUserId, eventId, new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc), "Newest"),
            CreateNotification(ownerUserId, eventId, new DateTime(2026, 7, 16, 11, 0, 0, DateTimeKind.Utc), "Middle"),
            CreateNotification(ownerUserId, eventId, new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc), "Oldest"),
            CreateNotification(Guid.CreateVersion7(), null, new DateTime(2026, 7, 16, 13, 0, 0, DateTimeKind.Utc), "Other owner"));
        await dbContext.SaveChangesAsync();

        var handler = new GetMyNotificationsHandler(dbContext, new TestCurrentUser(ownerUserId));

        var firstPage = await handler.HandleAsync(null, 2, CancellationToken.None);

        Assert.True(firstPage.IsSuccess);
        Assert.Equal(2, firstPage.Data!.Items.Count);
        Assert.Equal("Newest", firstPage.Data.Items[0].Title);
        Assert.Equal("Middle", firstPage.Data.Items[1].Title);
        Assert.Equal("Test Event", firstPage.Data.Items[0].EventTitle);
        Assert.True(firstPage.Data.HasMore);
        Assert.NotNull(firstPage.Data.NextCursor);

        var secondPage = await handler.HandleAsync(firstPage.Data.NextCursor, 2, CancellationToken.None);

        Assert.True(secondPage.IsSuccess);
        Assert.Single(secondPage.Data!.Items);
        Assert.Equal("Oldest", secondPage.Data.Items[0].Title);
        Assert.False(secondPage.Data.HasMore);
        Assert.Null(secondPage.Data.NextCursor);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadRequestForInvalidCursor()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetMyNotificationsHandler(
            dbContext,
            new TestCurrentUser(Guid.CreateVersion7()));

        var result = await handler.HandleAsync("not-a-valid-cursor", 20, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    private static Notification CreateNotification(
        Guid ownerUserId,
        Guid? eventId,
        DateTime createdAtUtc,
        string title) => new()
    {
        Id = Guid.CreateVersion7(),
        OwnerUserId = ownerUserId,
        EventId = eventId,
        Type = "Test",
        Severity = "Info",
        Title = title,
        Message = "Test message",
        ActionUrl = eventId.HasValue ? $"/events/{eventId}" : null,
        CreatedAtUtc = createdAtUtc
    };

    private static Event CreateEvent(Guid eventId, Guid ownerUserId) => new()
    {
        Id = eventId,
        OwnerUserId = ownerUserId,
        Title = "Test Event",
        EventDateUtc = new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc),
        GalleryToken = Guid.NewGuid().ToString("N"),
        UploadsEnabled = true,
        CreatedAtUtc = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc)
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
}