using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Events.GetEventRecentActivity;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Events;

public sealed class GetEventRecentActivityHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsLatestParticipantsAndPhotosWithSignedUrls()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participant1 = CreateParticipant(eventId, "Ayşe", new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc));
        var participant2 = CreateParticipant(eventId, "Mehmet", new DateTime(2026, 7, 15, 11, 0, 0, DateTimeKind.Utc));
        participant2.IsBlocked = true;
        participant2.BlockedAtUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.EventGuestParticipants.AddRange(participant1, participant2);
        dbContext.EventGuestPhotos.AddRange(
            CreatePhoto(eventId, participant1.Id, "photos/old", 100, new DateTime(2026, 7, 15, 10, 30, 0, DateTimeKind.Utc)),
            CreatePhoto(eventId, participant2.Id, "photos/new", 200, new DateTime(2026, 7, 15, 11, 30, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync();

        var now = new DateTimeOffset(2026, 7, 15, 13, 0, 0, TimeSpan.Zero);
        var handler = CreateHandler(dbContext, ownerUserId, now);

        var result = await handler.HandleAsync(
            eventId,
            new GetEventRecentActivityRequest { Limit = 2 },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([participant2.Id, participant1.Id], result.Data!.RecentParticipants.Select(x => x.ParticipantId));
        Assert.True(result.Data.RecentParticipants[0].IsBlocked);
        Assert.Equal(["Mehmet", "Ayşe"], result.Data.RecentPhotos.Select(x => x.ParticipantDisplayName));
        Assert.All(result.Data.RecentPhotos, x => Assert.StartsWith("https://storage.test/", x.ReadUrl));
        Assert.All(result.Data.RecentPhotos, x => Assert.Equal(now.AddMinutes(10).UtcDateTime, x.ReadUrlExpiresAtUtc));
    }

    [Fact]
    public async Task HandleAsync_AppliesLimitIndependentlyToBothLists()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));

        for (var index = 0; index < 3; index++)
        {
            var participant = CreateParticipant(
                eventId,
                $"Guest {index}",
                new DateTime(2026, 7, 15, 10 + index, 0, 0, DateTimeKind.Utc));
            dbContext.EventGuestParticipants.Add(participant);
            dbContext.EventGuestPhotos.Add(CreatePhoto(
                eventId,
                participant.Id,
                $"photos/{index}",
                100 + index,
                new DateTime(2026, 7, 15, 10 + index, 30, 0, DateTimeKind.Utc)));
        }

        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext, ownerUserId, DateTimeOffset.UtcNow);

        var result = await handler.HandleAsync(
            eventId,
            new GetEventRecentActivityRequest { Limit = 1 },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!.RecentParticipants);
        Assert.Single(result.Data.RecentPhotos);
        Assert.Equal("Guest 2", result.Data.RecentParticipants[0].DisplayName);
        Assert.Equal("Guest 2", result.Data.RecentPhotos[0].ParticipantDisplayName);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFoundForAnotherUsersEvent()
    {
        var eventId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, Guid.CreateVersion7()));
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext, Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        var result = await handler.HandleAsync(
            eventId,
            new GetEventRecentActivityRequest(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    private static GetEventRecentActivityHandler CreateHandler(
        OrtakareDbContext dbContext,
        Guid ownerUserId,
        DateTimeOffset now) => new(
            dbContext,
            new TestCurrentUser(ownerUserId),
            new TestObjectStorageService(),
            Options.Create(new ObjectStorageOptions { SignedUrlMinutes = 10 }),
            new TestTimeProvider(now));

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
        Title = "Test Event",
        EventDateUtc = new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc),
        GalleryToken = Guid.NewGuid().ToString("N"),
        UploadsEnabled = true,
        CreatedAtUtc = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc)
    };

    private static EventGuestParticipant CreateParticipant(Guid eventId, string name, DateTime createdAtUtc) => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = eventId,
        DisplayName = name,
        TokenHash = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
        CreatedAtUtc = createdAtUtc
    };

    private static EventGuestPhoto CreatePhoto(
        Guid eventId,
        Guid participantId,
        string storageKey,
        long size,
        DateTime createdAtUtc) => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = eventId,
        ParticipantId = participantId,
        ClientUploadId = Guid.CreateVersion7(),
        StorageKey = storageKey,
        OriginalFileName = "photo.jpg",
        ContentType = "image/jpeg",
        FileSizeBytes = size,
        CreatedAtUtc = createdAtUtc
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
