using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ortakare.Api.Features.Dashboard.GetOwnerRecentActivity;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Dashboard;

public sealed class GetOwnerRecentActivityHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsMergedOwnerActivityInDescendingOrder()
    {
        var ownerId = Guid.CreateVersion7();
        var otherOwnerId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var otherEventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        var otherParticipantId = Guid.CreateVersion7();
        var now = new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc);

        await using var dbContext = CreateDbContext();
        dbContext.Events.AddRange(
            CreateEvent(eventId, ownerId, "Owner Event"),
            CreateEvent(otherEventId, otherOwnerId, "Other Event"));
        dbContext.EventGuestParticipants.AddRange(
            CreateParticipant(participantId, eventId, "Ayşe", now.AddMinutes(-10)),
            CreateParticipant(otherParticipantId, otherEventId, "Other", now.AddMinutes(-1)));
        dbContext.EventGuestPhotos.AddRange(
            CreatePhoto(eventId, participantId, "owner/photo", now.AddMinutes(-5)),
            CreatePhoto(otherEventId, otherParticipantId, "other/photo", now));
        await dbContext.SaveChangesAsync();

        var storage = new TestObjectStorageService();
        var handler = new GetOwnerRecentActivityHandler(
            dbContext,
            new TestCurrentUser(ownerId),
            storage,
            Options.Create(new ObjectStorageOptions { SignedUrlMinutes = 10 }),
            new FixedTimeProvider(now));

        var result = await handler.HandleAsync(
            new GetOwnerRecentActivityRequest { Limit = 10 },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Items.Count);
        Assert.Equal("PhotoUploaded", result.Data.Items[0].ActivityType);
        Assert.Equal("ParticipantJoined", result.Data.Items[1].ActivityType);
        Assert.All(result.Data.Items, item => Assert.Equal(eventId, item.EventId));
        Assert.Equal("https://storage.test/owner/photo", result.Data.Items[0].ReadUrl);
        Assert.Null(result.Data.Items[1].ReadUrl);
        Assert.Single(storage.CreatedReadUrls);
    }

    [Fact]
    public async Task HandleAsync_AppliesLimitToMergedActivity()
    {
        var ownerId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        var now = new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc);

        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerId, "Event"));
        dbContext.EventGuestParticipants.Add(CreateParticipant(participantId, eventId, "Guest", now.AddMinutes(-3)));
        dbContext.EventGuestPhotos.AddRange(
            CreatePhoto(eventId, participantId, "photo/1", now.AddMinutes(-2)),
            CreatePhoto(eventId, participantId, "photo/2", now.AddMinutes(-1)));
        await dbContext.SaveChangesAsync();

        var handler = new GetOwnerRecentActivityHandler(
            dbContext,
            new TestCurrentUser(ownerId),
            new TestObjectStorageService(),
            Options.Create(new ObjectStorageOptions { SignedUrlMinutes = 10 }),
            new FixedTimeProvider(now));

        var result = await handler.HandleAsync(
            new GetOwnerRecentActivityRequest { Limit = 2 },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Items.Count);
        Assert.All(result.Data.Items, item => Assert.Equal("PhotoUploaded", item.ActivityType));
    }

    private static OrtakareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrtakareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrtakareDbContext(options);
    }

    private static Event CreateEvent(Guid id, Guid ownerId, string title) => new()
    {
        Id = id,
        OwnerUserId = ownerId,
        Title = title,
        EventDateUtc = new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc),
        GalleryToken = Guid.NewGuid().ToString("N"),
        UploadsEnabled = true,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static EventGuestParticipant CreateParticipant(
        Guid id,
        Guid eventId,
        string displayName,
        DateTime createdAtUtc) => new()
    {
        Id = id,
        EventId = eventId,
        DisplayName = displayName,
        TokenHash = Guid.NewGuid().ToString("N"),
        CreatedAtUtc = createdAtUtc
    };

    private static EventGuestPhoto CreatePhoto(
        Guid eventId,
        Guid participantId,
        string storageKey,
        DateTime createdAtUtc) => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = eventId,
        ParticipantId = participantId,
        ClientUploadId = Guid.CreateVersion7(),
        StorageKey = storageKey,
        OriginalFileName = "photo.jpg",
        ContentType = "image/jpeg",
        FileSizeBytes = 1000,
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

    private sealed class TestObjectStorageService : IObjectStorageService
    {
        public List<string> CreatedReadUrls { get; } = [];

        public string CreateReadUrl(string key, DateTime expiresAtUtc)
        {
            CreatedReadUrls.Add(key);
            return $"https://storage.test/{key}";
        }

        public Task UploadAsync(string key, Stream content, string contentType, long contentLength, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken)
            => Task.FromResult<Stream>(new MemoryStream());

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
