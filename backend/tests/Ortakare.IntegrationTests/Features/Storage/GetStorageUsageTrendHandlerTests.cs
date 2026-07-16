using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Features.Storage.GetStorageUsageTrend;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Storage;

public sealed class GetStorageUsageTrendHandlerTests
{
    private static readonly DateTime NowUtc = new(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleAsync_ReturnsThirtyDayTrendAndPeriodTotals()
    {
        var ownerUserId = Guid.CreateVersion7();
        var otherOwnerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var otherEventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        var otherParticipantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();

        dbContext.Events.AddRange(
            CreateEvent(eventId, ownerUserId),
            CreateEvent(otherEventId, otherOwnerUserId));
        dbContext.EventGuestParticipants.AddRange(
            CreateParticipant(participantId, eventId),
            CreateParticipant(otherParticipantId, otherEventId));
        dbContext.EventGuestPhotos.AddRange(
            CreatePhoto(eventId, participantId, 1_000, NowUtc),
            CreatePhoto(eventId, participantId, 2_000, NowUtc.AddDays(-2)),
            CreatePhoto(eventId, participantId, 3_000, NowUtc.AddDays(-10)),
            CreatePhoto(eventId, participantId, 9_000, NowUtc.AddDays(-30)),
            CreatePhoto(otherEventId, otherParticipantId, 50_000, NowUtc));
        await dbContext.SaveChangesAsync();

        var handler = new GetStorageUsageTrendHandler(
            dbContext,
            new TestCurrentUser(ownerUserId),
            new FixedTimeProvider(NowUtc));

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(30, result.Data.Days.Count);
        Assert.Equal(new DateOnly(2026, 6, 17), result.Data.Days[0].Date);
        Assert.Equal(new DateOnly(2026, 7, 16), result.Data.Days[^1].Date);
        Assert.Equal(1_000, result.Data.TodayAddedBytes);
        Assert.Equal(1, result.Data.TodayPhotoCount);
        Assert.Equal(3_000, result.Data.Last7DaysAddedBytes);
        Assert.Equal(2, result.Data.Last7DaysPhotoCount);
        Assert.Equal(6_000, result.Data.Last30DaysAddedBytes);
        Assert.Equal(3, result.Data.Last30DaysPhotoCount);
        Assert.Equal(2_000, result.Data.Days.Single(x => x.Date == new DateOnly(2026, 7, 14)).AddedBytes);
        Assert.Equal(0, result.Data.Days.Single(x => x.Date == new DateOnly(2026, 7, 15)).AddedBytes);
    }

    [Fact]
    public async Task HandleAsync_ReturnsZeroFilledTrendWhenOwnerHasNoPhotos()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetStorageUsageTrendHandler(
            dbContext,
            new TestCurrentUser(Guid.CreateVersion7()),
            new FixedTimeProvider(NowUtc));

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(30, result.Data!.Days.Count);
        Assert.All(result.Data.Days, day =>
        {
            Assert.Equal(0, day.AddedBytes);
            Assert.Equal(0, day.PhotoCount);
        });
        Assert.Equal(0, result.Data.Last30DaysAddedBytes);
    }

    private static OrtakareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrtakareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrtakareDbContext(options);
    }

    private static Event CreateEvent(Guid eventId, Guid ownerUserId) => new()
    {
        Id = eventId,
        OwnerUserId = ownerUserId,
        Title = "Trend Event",
        EventDateUtc = NowUtc.AddDays(10),
        GalleryToken = Guid.NewGuid().ToString("N"),
        UploadsEnabled = true,
        CreatedAtUtc = NowUtc.AddDays(-60)
    };

    private static EventGuestParticipant CreateParticipant(Guid participantId, Guid eventId) => new()
    {
        Id = participantId,
        EventId = eventId,
        DisplayName = "Guest",
        TokenHash = Guid.NewGuid().ToString("N"),
        CreatedAtUtc = NowUtc.AddDays(-20)
    };

    private static EventGuestPhoto CreatePhoto(
        Guid eventId,
        Guid participantId,
        long fileSizeBytes,
        DateTime createdAtUtc) => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = eventId,
        ParticipantId = participantId,
        ClientUploadId = Guid.CreateVersion7(),
        StorageKey = Guid.NewGuid().ToString("N"),
        OriginalFileName = "photo.jpg",
        ContentType = "image/jpeg",
        FileSizeBytes = fileSizeBytes,
        CreatedAtUtc = createdAtUtc
    };

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
