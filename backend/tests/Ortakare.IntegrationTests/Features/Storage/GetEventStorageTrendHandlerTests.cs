using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Features.Storage.GetEventStorageTrend;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Storage;

public sealed class GetEventStorageTrendHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsThirtyDayTrendForOwnedEvent()
    {
        var ownerId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();

        dbContext.Events.Add(CreateEvent(eventId, ownerId));
        dbContext.EventGuestParticipants.Add(CreateParticipant(participantId, eventId));
        dbContext.EventGuestPhotos.AddRange(
            CreatePhoto(eventId, participantId, now.UtcDateTime, 1_000),
            CreatePhoto(eventId, participantId, now.AddDays(-2).UtcDateTime, 2_000),
            CreatePhoto(eventId, participantId, now.AddDays(-10).UtcDateTime, 4_000),
            CreatePhoto(eventId, participantId, now.AddDays(-30).UtcDateTime, 8_000));
        await dbContext.SaveChangesAsync();

        var handler = new GetEventStorageTrendHandler(
            dbContext,
            new TestCurrentUser(ownerId),
            new FixedTimeProvider(now));

        var result = await handler.HandleAsync(eventId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(eventId, result.Data.EventId);
        Assert.Equal("Trend Event", result.Data.Title);
        Assert.Equal(30, result.Data.Days.Count);
        Assert.Equal(1_000, result.Data.TodayAddedBytes);
        Assert.Equal(1, result.Data.TodayPhotoCount);
        Assert.Equal(3_000, result.Data.Last7DaysAddedBytes);
        Assert.Equal(2, result.Data.Last7DaysPhotoCount);
        Assert.Equal(7_000, result.Data.Last30DaysAddedBytes);
        Assert.Equal(3, result.Data.Last30DaysPhotoCount);
        Assert.Contains(result.Data.Days, x => x.Date == new DateOnly(2026, 7, 15) && x.AddedBytes == 0);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFoundForAnotherOwnersEvent()
    {
        var eventId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, Guid.CreateVersion7()));
        await dbContext.SaveChangesAsync();

        var handler = new GetEventStorageTrendHandler(
            dbContext,
            new TestCurrentUser(Guid.CreateVersion7()),
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        var result = await handler.HandleAsync(eventId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsZeroFilledTrendForEmptyOwnedEvent()
    {
        var ownerId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerId));
        await dbContext.SaveChangesAsync();

        var handler = new GetEventStorageTrendHandler(
            dbContext,
            new TestCurrentUser(ownerId),
            new FixedTimeProvider(new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero)));

        var result = await handler.HandleAsync(eventId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(30, result.Data!.Days.Count);
        Assert.All(result.Data.Days, day =>
        {
            Assert.Equal(0, day.AddedBytes);
            Assert.Equal(0, day.PhotoCount);
        });
    }

    private static OrtakareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrtakareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrtakareDbContext(options);
    }

    private static Event CreateEvent(Guid id, Guid ownerId) => new()
    {
        Id = id,
        OwnerUserId = ownerId,
        Title = "Trend Event",
        EventDateUtc = new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc),
        GalleryToken = Guid.NewGuid().ToString("N"),
        UploadsEnabled = true,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static EventGuestParticipant CreateParticipant(Guid id, Guid eventId) => new()
    {
        Id = id,
        EventId = eventId,
        DisplayName = "Guest",
        TokenHash = Guid.NewGuid().ToString("N"),
        CreatedAtUtc = DateTime.UtcNow
    };

    private static EventGuestPhoto CreatePhoto(Guid eventId, Guid participantId, DateTime createdAtUtc, long bytes) => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = eventId,
        ParticipantId = participantId,
        ClientUploadId = Guid.CreateVersion7(),
        StorageKey = Guid.NewGuid().ToString("N"),
        OriginalFileName = "photo.jpg",
        ContentType = "image/jpeg",
        FileSizeBytes = bytes,
        CreatedAtUtc = createdAtUtc
    };

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
