using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Events.RegenerateGalleryToken;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Events;

public sealed class RegenerateGalleryTokenHandlerTests
{
    [Fact]
    public async Task HandleAsync_RegeneratesTokenForOwnedEvent()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var oldToken = "old-gallery-token";
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId, oldToken));
        await dbContext.SaveChangesAsync();

        var handler = new RegenerateGalleryTokenHandler(
            dbContext,
            new TestCurrentUser(ownerUserId),
            new TestTimeProvider(now));

        var result = await handler.HandleAsync(eventId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(oldToken, result.Data!.GalleryToken);
        Assert.True(result.Data.GalleryToken.Length >= 40);
        Assert.Equal(now.UtcDateTime, result.Data.UpdatedAtUtc);

        var persistedEvent = await dbContext.Events.SingleAsync(x => x.Id == eventId);
        Assert.Equal(result.Data.GalleryToken, persistedEvent.GalleryToken);
        Assert.Equal(now.UtcDateTime, persistedEvent.UpdatedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_InvalidatesPreviousPublicGalleryToken()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        const string oldToken = "old-gallery-token";
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId, oldToken));
        await dbContext.SaveChangesAsync();

        var handler = new RegenerateGalleryTokenHandler(
            dbContext,
            new TestCurrentUser(ownerUserId),
            new TestTimeProvider(DateTimeOffset.UtcNow));

        await handler.HandleAsync(eventId, CancellationToken.None);

        Assert.False(await dbContext.Events.AnyAsync(x => x.GalleryToken == oldToken));
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFoundForAnotherUsersEvent()
    {
        var eventId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, Guid.CreateVersion7(), "existing-token"));
        await dbContext.SaveChangesAsync();

        var handler = new RegenerateGalleryTokenHandler(
            dbContext,
            new TestCurrentUser(Guid.CreateVersion7()),
            new TestTimeProvider(DateTimeOffset.UtcNow));

        var result = await handler.HandleAsync(eventId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("existing-token", (await dbContext.Events.SingleAsync()).GalleryToken);
    }

    private static OrtakareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrtakareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrtakareDbContext(options);
    }

    private static Event CreateEvent(Guid eventId, Guid ownerUserId, string galleryToken) => new()
    {
        Id = eventId,
        OwnerUserId = ownerUserId,
        Title = "Test Event",
        EventDateUtc = new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc),
        GalleryToken = galleryToken,
        UploadsEnabled = true,
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
