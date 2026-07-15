using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Dashboard.GetOwnerDashboardSummary;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Dashboard;

public sealed class GetOwnerDashboardSummaryHandlerTests
{
    private static readonly DateTime NowUtc = new(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleAsync_ReturnsOnlyCurrentOwnersAggregatesAndUpcomingEvents()
    {
        var ownerUserId = Guid.CreateVersion7();
        var anotherOwnerUserId = Guid.CreateVersion7();
        var pastEventId = Guid.CreateVersion7();
        var upcomingOpenEventId = Guid.CreateVersion7();
        var upcomingClosedEventId = Guid.CreateVersion7();
        var anotherOwnersEventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        var blockedParticipantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();

        dbContext.Events.AddRange(
            CreateEvent(pastEventId, ownerUserId, "Past", NowUtc.AddDays(-1), true),
            CreateEvent(upcomingOpenEventId, ownerUserId, "Tomorrow", NowUtc.AddDays(1), true),
            CreateEvent(upcomingClosedEventId, ownerUserId, "Next Week", NowUtc.AddDays(7), false),
            CreateEvent(anotherOwnersEventId, anotherOwnerUserId, "Other", NowUtc.AddHours(1), true));

        dbContext.EventGuestParticipants.AddRange(
            CreateParticipant(participantId, upcomingOpenEventId, false),
            CreateParticipant(blockedParticipantId, upcomingClosedEventId, true),
            CreateParticipant(Guid.CreateVersion7(), anotherOwnersEventId, false));

        dbContext.EventGuestPhotos.AddRange(
            CreatePhoto(upcomingOpenEventId, participantId, 1_500),
            CreatePhoto(upcomingOpenEventId, participantId, 2_500),
            CreatePhoto(upcomingClosedEventId, blockedParticipantId, 4_000),
            CreatePhoto(anotherOwnersEventId, dbContext.EventGuestParticipants.Local.Last().Id, 50_000));
        await dbContext.SaveChangesAsync();

        var handler = new GetOwnerDashboardSummaryHandler(
            dbContext,
            new TestCurrentUser(ownerUserId),
            new FixedTimeProvider(NowUtc));

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.TotalEventCount);
        Assert.Equal(2, result.Data.UpcomingEventCount);
        Assert.Equal(1, result.Data.PastEventCount);
        Assert.Equal(2, result.Data.OpenEventCount);
        Assert.Equal(1, result.Data.ClosedEventCount);
        Assert.Equal(2, result.Data.ParticipantCount);
        Assert.Equal(1, result.Data.BlockedParticipantCount);
        Assert.Equal(3, result.Data.PhotoCount);
        Assert.Equal(8_000, result.Data.TotalPhotoSizeBytes);
        Assert.Equal(2, result.Data.UpcomingEvents.Count);
        Assert.Equal(upcomingOpenEventId, result.Data.UpcomingEvents[0].EventId);
        Assert.Equal(1, result.Data.UpcomingEvents[0].ParticipantCount);
        Assert.Equal(2, result.Data.UpcomingEvents[0].PhotoCount);
        Assert.Equal(upcomingClosedEventId, result.Data.UpcomingEvents[1].EventId);
    }

    [Fact]
    public async Task HandleAsync_ReturnsZeroValuesWhenOwnerHasNoEvents()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetOwnerDashboardSummaryHandler(
            dbContext,
            new TestCurrentUser(Guid.CreateVersion7()),
            new FixedTimeProvider(NowUtc));

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data.TotalEventCount);
        Assert.Equal(0, result.Data.ParticipantCount);
        Assert.Equal(0, result.Data.PhotoCount);
        Assert.Equal(0, result.Data.TotalPhotoSizeBytes);
        Assert.Empty(result.Data.UpcomingEvents);
    }

    [Fact]
    public async Task HandleAsync_ReturnsAtMostFiveNearestUpcomingEvents()
    {
        var ownerUserId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();

        for (var index = 1; index <= 7; index++)
        {
            dbContext.Events.Add(CreateEvent(
                Guid.CreateVersion7(),
                ownerUserId,
                $"Event {index}",
                NowUtc.AddDays(index),
                true));
        }

        await dbContext.SaveChangesAsync();

        var handler = new GetOwnerDashboardSummaryHandler(
            dbContext,
            new TestCurrentUser(ownerUserId),
            new FixedTimeProvider(NowUtc));

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Data!.UpcomingEventCount);
        Assert.Equal(5, result.Data.UpcomingEvents.Count);
        Assert.Equal("Event 1", result.Data.UpcomingEvents[0].Title);
        Assert.Equal("Event 5", result.Data.UpcomingEvents[4].Title);
    }

    private static OrtakareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrtakareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrtakareDbContext(options);
    }

    private static Event CreateEvent(
        Guid eventId,
        Guid ownerUserId,
        string title,
        DateTime eventDateUtc,
        bool uploadsEnabled) => new()
    {
        Id = eventId,
        OwnerUserId = ownerUserId,
        Title = title,
        EventDateUtc = eventDateUtc,
        GalleryToken = Guid.NewGuid().ToString("N"),
        UploadsEnabled = uploadsEnabled,
        CreatedAtUtc = NowUtc.AddDays(-10)
    };

    private static EventGuestParticipant CreateParticipant(
        Guid participantId,
        Guid eventId,
        bool isBlocked) => new()
    {
        Id = participantId,
        EventId = eventId,
        DisplayName = "Guest",
        TokenHash = Guid.NewGuid().ToString("N"),
        IsBlocked = isBlocked,
        BlockedAtUtc = isBlocked ? NowUtc : null,
        CreatedAtUtc = NowUtc
    };

    private static EventGuestPhoto CreatePhoto(
        Guid eventId,
        Guid participantId,
        long fileSizeBytes) => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = eventId,
        ParticipantId = participantId,
        ClientUploadId = Guid.CreateVersion7(),
        StorageKey = Guid.NewGuid().ToString("N"),
        OriginalFileName = "photo.jpg",
        ContentType = "image/jpeg",
        FileSizeBytes = fileSizeBytes,
        CreatedAtUtc = NowUtc
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
