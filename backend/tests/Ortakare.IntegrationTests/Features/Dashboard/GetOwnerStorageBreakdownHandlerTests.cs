using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Dashboard.GetOwnerStorageBreakdown;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Dashboard;

public sealed class GetOwnerStorageBreakdownHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsOwnerStorageOverviewAndPagedBreakdown()
    {
        var ownerUserId = Guid.CreateVersion7();
        var otherOwnerUserId = Guid.CreateVersion7();
        var firstEventId = Guid.CreateVersion7();
        var secondEventId = Guid.CreateVersion7();
        var thirdEventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        var firstUploadAtUtc = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc);
        var lastUploadAtUtc = new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc);

        await using var dbContext = CreateDbContext();
        dbContext.Events.AddRange(
            CreateEvent(firstEventId, ownerUserId, "Large Event", true),
            CreateEvent(secondEventId, ownerUserId, "Many Photos", false),
            CreateEvent(thirdEventId, ownerUserId, "Empty Event", true),
            CreateEvent(Guid.CreateVersion7(), otherOwnerUserId, "Other Owner", true));

        dbContext.EventGuestPhotos.AddRange(
            CreatePhoto(firstEventId, participantId, 5_000, firstUploadAtUtc),
            CreatePhoto(firstEventId, participantId, 4_000, lastUploadAtUtc),
            CreatePhoto(secondEventId, participantId, 2_000, firstUploadAtUtc),
            CreatePhoto(secondEventId, participantId, 2_000, firstUploadAtUtc),
            CreatePhoto(secondEventId, participantId, 2_000, firstUploadAtUtc),
            CreatePhoto(dbContext.Events.Local.Single(x => x.OwnerUserId == otherOwnerUserId).Id, participantId, 100_000, lastUploadAtUtc));
        await dbContext.SaveChangesAsync();

        var handler = new GetOwnerStorageBreakdownHandler(
            dbContext,
            new TestCurrentUser(ownerUserId));

        var result = await handler.HandleAsync(
            new GetOwnerStorageBreakdownRequest { Page = 1, PageSize = 2 },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.TotalCount);
        Assert.Equal(2, result.Data.Items.Count);
        Assert.Equal(2, result.Data.TotalPages);
        Assert.Equal(3, result.Data.Overview.TotalEventCount);
        Assert.Equal(5, result.Data.Overview.TotalPhotoCount);
        Assert.Equal(15_000, result.Data.Overview.TotalStorageBytes);
        Assert.Equal(5_000, result.Data.Overview.AverageStoragePerEventBytes);
        Assert.Equal(3_000, result.Data.Overview.AveragePhotoSizeBytes);
        Assert.Equal(lastUploadAtUtc, result.Data.Overview.LastUploadAtUtc);
        Assert.Equal(firstEventId, result.Data.Overview.LargestStorageEvent!.EventId);
        Assert.Equal(9_000, result.Data.Overview.LargestStorageEvent.TotalStorageBytes);
        Assert.Equal(secondEventId, result.Data.Overview.MostPhotosEvent!.EventId);
        Assert.Equal(3, result.Data.Overview.MostPhotosEvent.PhotoCount);
        Assert.Equal(firstEventId, result.Data.Items[0].EventId);
        Assert.Equal(4_500, result.Data.Items[0].AveragePhotoSizeBytes);
        Assert.Equal(secondEventId, result.Data.Items[1].EventId);
        Assert.False(result.Data.Items[1].UploadsEnabled);
    }

    [Fact]
    public async Task HandleAsync_ReturnsZerosForOwnerWithoutEvents()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetOwnerStorageBreakdownHandler(
            dbContext,
            new TestCurrentUser(Guid.CreateVersion7()));

        var result = await handler.HandleAsync(
            new GetOwnerStorageBreakdownRequest(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data.Items);
        Assert.Equal(0, result.Data.TotalCount);
        Assert.Equal(0, result.Data.TotalPages);
        Assert.Equal(0, result.Data.Overview.TotalStorageBytes);
        Assert.Equal(0, result.Data.Overview.TotalPhotoCount);
        Assert.Null(result.Data.Overview.LargestStorageEvent);
        Assert.Null(result.Data.Overview.MostPhotosEvent);
        Assert.Null(result.Data.Overview.LastUploadAtUtc);
    }

    private static OrtakareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrtakareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrtakareDbContext(options);
    }

    private static Event CreateEvent(Guid id, Guid ownerUserId, string title, bool uploadsEnabled) => new()
    {
        Id = id,
        OwnerUserId = ownerUserId,
        Title = title,
        EventDateUtc = new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc),
        GalleryToken = Guid.NewGuid().ToString("N"),
        UploadsEnabled = uploadsEnabled,
        CreatedAtUtc = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc)
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
