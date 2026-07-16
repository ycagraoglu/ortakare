using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Features.Storage.GetParticipantStorageBreakdown;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Storage;

public sealed class GetParticipantStorageBreakdownHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsParticipantsOrderedByStorageUsage()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var firstParticipantId = Guid.CreateVersion7();
        var secondParticipantId = Guid.CreateVersion7();
        var emptyParticipantId = Guid.CreateVersion7();
        var now = new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc);

        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.EventGuestParticipants.AddRange(
            CreateParticipant(firstParticipantId, eventId, "Ali", false, now.AddDays(-3)),
            CreateParticipant(secondParticipantId, eventId, "Ayşe", true, now.AddDays(-2)),
            CreateParticipant(emptyParticipantId, eventId, "Mehmet", false, now.AddDays(-1)));
        dbContext.EventGuestPhotos.AddRange(
            CreatePhoto(eventId, firstParticipantId, 1_000, now.AddHours(-3)),
            CreatePhoto(eventId, firstParticipantId, 2_000, now.AddHours(-1)),
            CreatePhoto(eventId, secondParticipantId, 500, now.AddHours(-2)));
        await dbContext.SaveChangesAsync();

        var handler = new GetParticipantStorageBreakdownHandler(
            dbContext,
            new TestCurrentUser(ownerUserId));

        var result = await handler.HandleAsync(
            eventId,
            new GetParticipantStorageBreakdownRequest(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(eventId, result.Data.EventId);
        Assert.Equal("Storage Event", result.Data.EventTitle);
        Assert.Equal(3_500, result.Data.TotalStorageBytes);
        Assert.Equal(3, result.Data.TotalPhotoCount);
        Assert.Equal(3, result.Data.TotalCount);
        Assert.Equal(firstParticipantId, result.Data.Items[0].ParticipantId);
        Assert.Equal(2, result.Data.Items[0].PhotoCount);
        Assert.Equal(3_000, result.Data.Items[0].TotalStorageBytes);
        Assert.Equal(1_500, result.Data.Items[0].AveragePhotoSizeBytes);
        Assert.Equal(now.AddHours(-1), result.Data.Items[0].LastUploadAtUtc);
        Assert.Equal(secondParticipantId, result.Data.Items[1].ParticipantId);
        Assert.True(result.Data.Items[1].IsBlocked);
        Assert.Equal(emptyParticipantId, result.Data.Items[2].ParticipantId);
        Assert.Equal(0, result.Data.Items[2].PhotoCount);
        Assert.Equal(0, result.Data.Items[2].TotalStorageBytes);
        Assert.Null(result.Data.Items[2].LastUploadAtUtc);
    }

    [Fact]
    public async Task HandleAsync_AppliesPagination()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));

        for (var index = 0; index < 5; index++)
        {
            dbContext.EventGuestParticipants.Add(CreateParticipant(
                Guid.CreateVersion7(),
                eventId,
                $"Guest {index}",
                false,
                DateTime.UtcNow.AddMinutes(index)));
        }

        await dbContext.SaveChangesAsync();

        var handler = new GetParticipantStorageBreakdownHandler(
            dbContext,
            new TestCurrentUser(ownerUserId));

        var result = await handler.HandleAsync(
            eventId,
            new GetParticipantStorageBreakdownRequest { Page = 2, PageSize = 2 },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Items.Count);
        Assert.Equal(5, result.Data.TotalCount);
        Assert.Equal(3, result.Data.TotalPages);
        Assert.Equal(2, result.Data.Page);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFoundForAnotherOwnersEvent()
    {
        var eventId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, Guid.CreateVersion7()));
        await dbContext.SaveChangesAsync();

        var handler = new GetParticipantStorageBreakdownHandler(
            dbContext,
            new TestCurrentUser(Guid.CreateVersion7()));

        var result = await handler.HandleAsync(
            eventId,
            new GetParticipantStorageBreakdownRequest(),
            CancellationToken.None);

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

    private static EventGuestParticipant CreateParticipant(
        Guid participantId,
        Guid eventId,
        string displayName,
        bool isBlocked,
        DateTime createdAtUtc) => new()
    {
        Id = participantId,
        EventId = eventId,
        DisplayName = displayName,
        TokenHash = Guid.NewGuid().ToString("N"),
        IsBlocked = isBlocked,
        BlockedAtUtc = isBlocked ? createdAtUtc.AddHours(1) : null,
        CreatedAtUtc = createdAtUtc
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
}
