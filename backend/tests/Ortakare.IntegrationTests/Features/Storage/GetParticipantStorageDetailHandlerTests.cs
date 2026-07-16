using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Features.Storage.GetParticipantStorageDetail;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Storage;

public sealed class GetParticipantStorageDetailHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsParticipantStorageDetail()
    {
        var now = new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();

        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.EventGuestParticipants.Add(CreateParticipant(participantId, eventId));
        dbContext.EventGuestPhotos.AddRange(
            CreatePhoto(eventId, participantId, 1_000, now.UtcDateTime.AddDays(-2), "a.jpg"),
            CreatePhoto(eventId, participantId, 3_000, now.UtcDateTime.AddDays(-1), "b.jpg"),
            CreatePhoto(eventId, participantId, 5_000, now.UtcDateTime, "c.jpg"));
        await dbContext.SaveChangesAsync();

        var handler = new GetParticipantStorageDetailHandler(
            dbContext,
            new TestCurrentUser(ownerUserId),
            new TestObjectStorageService(),
            CreateConfiguration(),
            new TestTimeProvider(now));

        var result = await handler.HandleAsync(eventId, participantId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.PhotoCount);
        Assert.Equal(9_000, result.Data.TotalStorageBytes);
        Assert.Equal(3_000, result.Data.AveragePhotoSizeBytes);
        Assert.Equal(now.UtcDateTime.AddDays(-2), result.Data.FirstUploadAtUtc);
        Assert.Equal(now.UtcDateTime, result.Data.LastUploadAtUtc);
        Assert.Equal(30, result.Data.Days.Count);
        Assert.Equal(3, result.Data.RecentPhotos.Count);
        Assert.Equal("c.jpg", result.Data.RecentPhotos[0].OriginalFileName);
        Assert.Equal(now.UtcDateTime.AddMinutes(15), result.Data.RecentPhotos[0].ReadUrlExpiresAtUtc);
        Assert.StartsWith("https://signed.test/", result.Data.RecentPhotos[0].ReadUrl);
    }

    [Fact]
    public async Task HandleAsync_ReturnsZeroValuesForParticipantWithoutPhotos()
    {
        var now = new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();

        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.EventGuestParticipants.Add(CreateParticipant(participantId, eventId));
        await dbContext.SaveChangesAsync();

        var handler = new GetParticipantStorageDetailHandler(
            dbContext,
            new TestCurrentUser(ownerUserId),
            new TestObjectStorageService(),
            CreateConfiguration(),
            new TestTimeProvider(now));

        var result = await handler.HandleAsync(eventId, participantId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Data!.PhotoCount);
        Assert.Equal(0, result.Data.TotalStorageBytes);
        Assert.Equal(0, result.Data.AveragePhotoSizeBytes);
        Assert.Null(result.Data.FirstUploadAtUtc);
        Assert.Null(result.Data.LastUploadAtUtc);
        Assert.Empty(result.Data.RecentPhotos);
        Assert.All(result.Data.Days, day =>
        {
            Assert.Equal(0, day.AddedBytes);
            Assert.Equal(0, day.PhotoCount);
        });
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFoundForAnotherOwnersParticipant()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();

        dbContext.Events.Add(CreateEvent(eventId, Guid.CreateVersion7()));
        dbContext.EventGuestParticipants.Add(CreateParticipant(participantId, eventId));
        await dbContext.SaveChangesAsync();

        var handler = new GetParticipantStorageDetailHandler(
            dbContext,
            new TestCurrentUser(ownerUserId),
            new TestObjectStorageService(),
            CreateConfiguration(),
            new TestTimeProvider(DateTimeOffset.UtcNow));

        var result = await handler.HandleAsync(eventId, participantId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    private static OrtakareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrtakareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrtakareDbContext(options);
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ObjectStorage:SignedUrlMinutes"] = "15"
            })
            .Build();

    private static Event CreateEvent(Guid eventId, Guid ownerUserId) => new()
    {
        Id = eventId,
        OwnerUserId = ownerUserId,
        Title = "Storage Event",
        EventDateUtc = new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc),
        GalleryToken = Guid.NewGuid().ToString("N"),
        UploadsEnabled = true,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static EventGuestParticipant CreateParticipant(Guid participantId, Guid eventId) => new()
    {
        Id = participantId,
        EventId = eventId,
        DisplayName = "Guest",
        TokenHash = Guid.NewGuid().ToString("N"),
        CreatedAtUtc = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc)
    };

    private static EventGuestPhoto CreatePhoto(
        Guid eventId,
        Guid participantId,
        long sizeBytes,
        DateTime createdAtUtc,
        string fileName) => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = eventId,
        ParticipantId = participantId,
        ClientUploadId = Guid.CreateVersion7(),
        StorageKey = $"storage/{Guid.NewGuid():N}",
        OriginalFileName = fileName,
        ContentType = "image/jpeg",
        FileSizeBytes = sizeBytes,
        CreatedAtUtc = createdAtUtc
    };

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class TestObjectStorageService : IObjectStorageService
    {
        public Task UploadAsync(string key, Stream content, string contentType, long contentLength, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult<Stream>(new MemoryStream());

        public Task DeleteAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;

        public string CreateReadUrl(string key, DateTime expiresAtUtc) => $"https://signed.test/{key}";
    }
}
