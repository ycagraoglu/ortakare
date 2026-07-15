using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Participants.DeleteEventParticipant;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Participants;

public sealed class DeleteEventParticipantHandlerTests
{
    [Fact]
    public async Task HandleAsync_DeletesParticipantAndOwnedPhotos()
    {
        var ownerId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        var storage = new TestObjectStorageService();
        Seed(dbContext, eventId, ownerId, participantId, photoCount: 2);
        await dbContext.SaveChangesAsync();

        foreach (var photo in dbContext.EventGuestPhotos)
            await storage.UploadAsync(photo.StorageKey, new MemoryStream([1, 2, 3]), photo.ContentType, 3, CancellationToken.None);

        var handler = CreateHandler(dbContext, ownerId, storage);
        var result = await handler.HandleAsync(eventId, participantId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.DeletedPhotoCount);
        Assert.False(await dbContext.EventGuestParticipants.AnyAsync(x => x.Id == participantId));
        Assert.False(await dbContext.EventGuestPhotos.AnyAsync(x => x.ParticipantId == participantId));
        Assert.Empty(storage.Objects);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFoundForAnotherOwnersEvent()
    {
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        Seed(dbContext, eventId, Guid.CreateVersion7(), participantId, photoCount: 0);
        await dbContext.SaveChangesAsync();

        var result = await CreateHandler(dbContext, Guid.CreateVersion7(), new TestObjectStorageService())
            .HandleAsync(eventId, participantId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.True(await dbContext.EventGuestParticipants.AnyAsync(x => x.Id == participantId));
    }

    [Fact]
    public async Task HandleAsync_PreservesDatabaseWhenStorageDeletionFails()
    {
        var ownerId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        Seed(dbContext, eventId, ownerId, participantId, photoCount: 1);
        await dbContext.SaveChangesAsync();
        var storage = new TestObjectStorageService { ThrowOnDelete = true };

        var result = await CreateHandler(dbContext, ownerId, storage)
            .HandleAsync(eventId, participantId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(503, result.StatusCode);
        Assert.True(await dbContext.EventGuestParticipants.AnyAsync(x => x.Id == participantId));
        Assert.True(await dbContext.EventGuestPhotos.AnyAsync(x => x.ParticipantId == participantId));
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFoundWhenParticipantBelongsToAnotherEvent()
    {
        var ownerId = Guid.CreateVersion7();
        var requestedEventId = Guid.CreateVersion7();
        var otherEventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        Seed(dbContext, requestedEventId, ownerId, Guid.CreateVersion7(), photoCount: 0);
        Seed(dbContext, otherEventId, ownerId, participantId, photoCount: 0);
        await dbContext.SaveChangesAsync();

        var result = await CreateHandler(dbContext, ownerId, new TestObjectStorageService())
            .HandleAsync(requestedEventId, participantId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.True(await dbContext.EventGuestParticipants.AnyAsync(x => x.Id == participantId));
    }

    private static DeleteEventParticipantHandler CreateHandler(
        OrtakareDbContext dbContext,
        Guid ownerId,
        TestObjectStorageService storage) =>
        new(dbContext, new TestCurrentUser(ownerId), storage, NullLogger<DeleteEventParticipantHandler>.Instance);

    private static OrtakareDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<OrtakareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static void Seed(
        OrtakareDbContext dbContext,
        Guid eventId,
        Guid ownerId,
        Guid participantId,
        int photoCount)
    {
        dbContext.Events.Add(new Event
        {
            Id = eventId,
            OwnerUserId = ownerId,
            Title = "Test Event",
            EventDateUtc = DateTime.UtcNow.AddDays(1),
            GalleryToken = Guid.NewGuid().ToString("N"),
            UploadsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        dbContext.EventGuestParticipants.Add(new EventGuestParticipant
        {
            Id = participantId,
            EventId = eventId,
            DisplayName = "Guest",
            TokenHash = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTime.UtcNow
        });

        for (var index = 0; index < photoCount; index++)
        {
            dbContext.EventGuestPhotos.Add(new EventGuestPhoto
            {
                Id = Guid.CreateVersion7(),
                EventId = eventId,
                ParticipantId = participantId,
                ClientUploadId = Guid.CreateVersion7(),
                StorageKey = $"events/{eventId:N}/photos/{Guid.NewGuid():N}.jpg",
                OriginalFileName = $"photo-{index}.jpg",
                ContentType = "image/jpeg",
                FileSizeBytes = 3,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }
}
