using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Events.ReopenEvent;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Events;

public sealed class ReopenEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReopensOwnedEvent()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId, uploadsEnabled: false));
        await dbContext.SaveChangesAsync();

        var handler = new ReopenEventHandler(dbContext, new TestCurrentUser(ownerUserId), new TestTimeProvider(now));

        var result = await handler.HandleAsync(eventId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.UploadsEnabled);
        Assert.Equal(now.UtcDateTime, result.Data.UpdatedAtUtc);
        Assert.True((await dbContext.Events.SingleAsync(x => x.Id == eventId)).UploadsEnabled);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFoundForAnotherUsersEvent()
    {
        var eventId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, Guid.CreateVersion7(), uploadsEnabled: false));
        await dbContext.SaveChangesAsync();

        var handler = new ReopenEventHandler(
            dbContext,
            new TestCurrentUser(Guid.CreateVersion7()),
            new TestTimeProvider(DateTimeOffset.UtcNow));

        var result = await handler.HandleAsync(eventId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_IsIdempotentWhenEventIsAlreadyOpen()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var existingUpdatedAt = new DateTime(2026, 7, 10, 9, 0, 0, DateTimeKind.Utc);
        await using var dbContext = CreateDbContext();
        var eventEntity = CreateEvent(eventId, ownerUserId, uploadsEnabled: true);
        eventEntity.UpdatedAtUtc = existingUpdatedAt;
        dbContext.Events.Add(eventEntity);
        await dbContext.SaveChangesAsync();

        var handler = new ReopenEventHandler(
            dbContext,
            new TestCurrentUser(ownerUserId),
            new TestTimeProvider(new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero)));

        var result = await handler.HandleAsync(eventId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.UploadsEnabled);
        Assert.Equal(existingUpdatedAt, result.Data.UpdatedAtUtc);
    }

    private static OrtakareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrtakareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrtakareDbContext(options);
    }

    private static Event CreateEvent(Guid eventId, Guid ownerUserId, bool uploadsEnabled) => new()
    {
        Id = eventId,
        OwnerUserId = ownerUserId,
        Title = "Test Event",
        EventDateUtc = new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc),
        GalleryToken = Guid.NewGuid().ToString("N"),
        UploadsEnabled = uploadsEnabled,
        CreatedAtUtc = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc)
    };

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }

    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
