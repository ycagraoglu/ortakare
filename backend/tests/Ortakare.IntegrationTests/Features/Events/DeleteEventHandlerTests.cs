using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Events.DeleteEvent;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Events;

public sealed class DeleteEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_DeletesOwnedEventAndStoredObjects()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        var storage = new RecordingObjectStorageService();

        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.EventGuestParticipants.Add(CreateParticipant(participantId, eventId));
        dbContext.EventGuestPhotos.Add(CreatePhoto(eventId, participantId, "photos/event/photo.jpg"));
        dbContext.GalleryExports.Add(CreateExport(eventId, GalleryExportStatus.Completed, "exports/event/export.zip"));
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext, ownerUserId, storage);

        var result = await handler.HandleAsync(eventId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.DeletedPhotoCount);
        Assert.Equal(1, result.Data.DeletedParticipantCount);
        Assert.Equal(1, result.Data.DeletedExportCount);
        Assert.Contains("photos/event/photo.jpg", storage.DeletedKeys);
        Assert.Contains("exports/event/export.zip", storage.DeletedKeys);
        Assert.False(await dbContext.Events.AnyAsync(x => x.Id == eventId));
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflictWhenExportIsActive()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        var storage = new RecordingObjectStorageService();

        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.GalleryExports.Add(CreateExport(eventId, GalleryExportStatus.Processing, null));
        await dbContext.SaveChangesAsync();

        var result = await CreateHandler(dbContext, ownerUserId, storage)
            .HandleAsync(eventId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
        Assert.True(await dbContext.Events.AnyAsync(x => x.Id == eventId));
        Assert.Empty(storage.DeletedKeys);
    }

    [Fact]
    public async Task HandleAsync_PreservesDatabaseWhenStorageDeletionFails()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        var storage = new RecordingObjectStorageService { ThrowOnDelete = true };

        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.EventGuestParticipants.Add(CreateParticipant(participantId, eventId));
        dbContext.EventGuestPhotos.Add(CreatePhoto(eventId, participantId, "photos/event/photo.jpg"));
        await dbContext.SaveChangesAsync();

        var result = await CreateHandler(dbContext, ownerUserId, storage)
            .HandleAsync(eventId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(503, result.StatusCode);
        Assert.True(await dbContext.Events.AnyAsync(x => x.Id == eventId));
        Assert.True(await dbContext.EventGuestPhotos.AnyAsync(x => x.EventId == eventId));
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFoundForAnotherUsersEvent()
    {
        var eventId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, Guid.CreateVersion7()));
        await dbContext.SaveChangesAsync();

        var result = await CreateHandler(
                dbContext,
                Guid.CreateVersion7(),
                new RecordingObjectStorageService())
            .HandleAsync(eventId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.True(await dbContext.Events.AnyAsync(x => x.Id == eventId));
    }

    private static DeleteEventHandler CreateHandler(
        OrtakareDbContext dbContext,
        Guid ownerUserId,
        IObjectStorageService storage) =>
        new(
            dbContext,
            new TestCurrentUser(ownerUserId),
            storage,
            NullLogger<DeleteEventHandler>.Instance);

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
        UploadsEnabled = false,
        CreatedAtUtc = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc)
    };

    private static EventGuestParticipant CreateParticipant(Guid participantId, Guid eventId) => new()
    {
        Id = participantId,
        EventId = eventId,
        DisplayName = "Guest",
        TokenHash = Guid.NewGuid().ToString("N"),
        CreatedAtUtc = DateTime.UtcNow
    };

    private static EventGuestPhoto CreatePhoto(Guid eventId, Guid participantId, string storageKey) => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = eventId,
        ParticipantId = participantId,
        ClientUploadId = Guid.CreateVersion7(),
        StorageKey = storageKey,
        OriginalFileName = "photo.jpg",
        ContentType = "image/jpeg",
        FileSizeBytes = 128,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static GalleryExport CreateExport(
        Guid eventId,
        GalleryExportStatus status,
        string? storageKey) => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = eventId,
        Status = status,
        PhotoCount = 1,
        StorageKey = storageKey,
        CreatedAtUtc = DateTime.UtcNow
    };

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }

    private sealed class RecordingObjectStorageService : IObjectStorageService
    {
        public List<string> DeletedKeys { get; } = [];
        public bool ThrowOnDelete { get; init; }

        public Task UploadAsync(string key, Stream content, string contentType, long contentLength, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            if (ThrowOnDelete)
            {
                throw new InvalidOperationException("Simulated storage failure.");
            }

            DeletedKeys.Add(key);
            return Task.CompletedTask;
        }

        public string CreateReadUrl(string key, DateTime expiresAtUtc) =>
            throw new NotSupportedException();
    }
}
