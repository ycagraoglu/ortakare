using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Events.GetEventSummary;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Events;

public sealed class GetEventSummaryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsAggregatedEventSummary()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var activeParticipantId = Guid.CreateVersion7();
        var blockedParticipantId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();

        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.EventGuestParticipants.AddRange(
            CreateParticipant(activeParticipantId, eventId, false),
            CreateParticipant(blockedParticipantId, eventId, true));
        dbContext.EventGuestPhotos.AddRange(
            CreatePhoto(eventId, activeParticipantId, 1_500),
            CreatePhoto(eventId, blockedParticipantId, 2_500));
        dbContext.GalleryExports.AddRange(
            CreateExport(eventId, GalleryExportStatus.Pending),
            CreateExport(eventId, GalleryExportStatus.Completed),
            CreateExport(eventId, GalleryExportStatus.Completed),
            CreateExport(eventId, GalleryExportStatus.Failed),
            CreateExport(eventId, GalleryExportStatus.Cancelled));
        await dbContext.SaveChangesAsync();

        var handler = new GetEventSummaryHandler(dbContext, new TestCurrentUser(ownerUserId));

        var result = await handler.HandleAsync(eventId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.ParticipantCount);
        Assert.Equal(1, result.Data.BlockedParticipantCount);
        Assert.Equal(2, result.Data.PhotoCount);
        Assert.Equal(4_000, result.Data.TotalPhotoSizeBytes);
        Assert.Equal(5, result.Data.ExportSummary.TotalCount);
        Assert.Equal(1, result.Data.ExportSummary.PendingCount);
        Assert.Equal(0, result.Data.ExportSummary.ProcessingCount);
        Assert.Equal(2, result.Data.ExportSummary.CompletedCount);
        Assert.Equal(1, result.Data.ExportSummary.FailedCount);
        Assert.Equal(1, result.Data.ExportSummary.CancelledCount);
    }

    [Fact]
    public async Task HandleAsync_ReturnsZeroCountsForEmptyEvent()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        await dbContext.SaveChangesAsync();

        var handler = new GetEventSummaryHandler(dbContext, new TestCurrentUser(ownerUserId));

        var result = await handler.HandleAsync(eventId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Data!.ParticipantCount);
        Assert.Equal(0, result.Data.BlockedParticipantCount);
        Assert.Equal(0, result.Data.PhotoCount);
        Assert.Equal(0, result.Data.TotalPhotoSizeBytes);
        Assert.Equal(0, result.Data.ExportSummary.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFoundForAnotherOwnersEvent()
    {
        var eventId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, Guid.CreateVersion7()));
        await dbContext.SaveChangesAsync();

        var handler = new GetEventSummaryHandler(dbContext, new TestCurrentUser(Guid.CreateVersion7()));

        var result = await handler.HandleAsync(eventId, CancellationToken.None);

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
        Title = "Summary Event",
        EventDateUtc = new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc),
        GalleryToken = Guid.NewGuid().ToString("N"),
        UploadsEnabled = true,
        CreatedAtUtc = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc)
    };

    private static EventGuestParticipant CreateParticipant(Guid participantId, Guid eventId, bool isBlocked) => new()
    {
        Id = participantId,
        EventId = eventId,
        DisplayName = "Guest",
        TokenHash = Guid.NewGuid().ToString("N"),
        IsBlocked = isBlocked,
        BlockedAtUtc = isBlocked ? DateTime.UtcNow : null,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static EventGuestPhoto CreatePhoto(Guid eventId, Guid participantId, long sizeBytes) => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = eventId,
        ParticipantId = participantId,
        ClientUploadId = Guid.CreateVersion7(),
        StorageKey = Guid.NewGuid().ToString("N"),
        OriginalFileName = "photo.jpg",
        ContentType = "image/jpeg",
        FileSizeBytes = sizeBytes,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static GalleryExport CreateExport(Guid eventId, GalleryExportStatus status) => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = eventId,
        Status = status,
        PhotoCount = 2,
        CreatedAtUtc = DateTime.UtcNow,
        CancelledAtUtc = status == GalleryExportStatus.Cancelled ? DateTime.UtcNow : null
    };

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }
}
