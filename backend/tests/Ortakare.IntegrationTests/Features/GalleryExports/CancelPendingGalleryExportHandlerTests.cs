using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Features.GalleryExports.CancelPendingGalleryExport;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.BackgroundJobs;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.GalleryExports;

public sealed class CancelPendingGalleryExportHandlerTests
{
    [Fact]
    public async Task HandleAsync_CancelsPendingExport()
    {
        var ownerId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var exportId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 7, 15, 20, 0, 0, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerId));
        dbContext.GalleryExports.Add(CreateExport(exportId, eventId, GalleryExportStatus.Pending));
        await dbContext.SaveChangesAsync();

        var handler = new CancelPendingGalleryExportHandler(
            dbContext,
            new TestCurrentUser(ownerId),
            new TestTimeProvider(now));

        var result = await handler.HandleAsync(eventId, exportId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(GalleryExportStatus.Cancelled, result.Data!.Status);
        Assert.Equal(now.UtcDateTime, result.Data.CancelledAtUtc);
        var stored = await dbContext.GalleryExports.SingleAsync(x => x.Id == exportId);
        Assert.Equal(GalleryExportStatus.Cancelled, stored.Status);
        Assert.Equal(now.UtcDateTime, stored.CancelledAtUtc);
    }

    [Fact]
    public async Task HandleAsync_IsIdempotentWhenAlreadyCancelled()
    {
        var ownerId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var exportId = Guid.CreateVersion7();
        var cancelledAt = new DateTime(2026, 7, 15, 18, 0, 0, DateTimeKind.Utc);
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerId));
        var galleryExport = CreateExport(exportId, eventId, GalleryExportStatus.Cancelled);
        galleryExport.CancelledAtUtc = cancelledAt;
        dbContext.GalleryExports.Add(galleryExport);
        await dbContext.SaveChangesAsync();

        var handler = new CancelPendingGalleryExportHandler(
            dbContext,
            new TestCurrentUser(ownerId),
            new TestTimeProvider(new DateTimeOffset(2026, 7, 15, 21, 0, 0, TimeSpan.Zero)));

        var result = await handler.HandleAsync(eventId, exportId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(cancelledAt, result.Data!.CancelledAtUtc);
    }

    [Theory]
    [InlineData(GalleryExportStatus.Processing)]
    [InlineData(GalleryExportStatus.Completed)]
    [InlineData(GalleryExportStatus.Failed)]
    public async Task HandleAsync_ReturnsConflictForNonPendingStatus(GalleryExportStatus status)
    {
        var ownerId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var exportId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerId));
        dbContext.GalleryExports.Add(CreateExport(exportId, eventId, status));
        await dbContext.SaveChangesAsync();

        var handler = new CancelPendingGalleryExportHandler(
            dbContext,
            new TestCurrentUser(ownerId),
            new TestTimeProvider(DateTimeOffset.UtcNow));

        var result = await handler.HandleAsync(eventId, exportId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task BuildJob_SkipsCancelledExportWithoutStorageAccess()
    {
        var eventId = Guid.CreateVersion7();
        var exportId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.GalleryExports.Add(CreateExport(exportId, eventId, GalleryExportStatus.Cancelled));
        await dbContext.SaveChangesAsync();
        var storage = new TestObjectStorageService { ThrowOnRead = true };
        var job = new BuildGalleryExportJob(
            dbContext,
            storage,
            TimeProvider.System,
            NullLogger<BuildGalleryExportJob>.Instance);

        await job.ExecuteAsync(exportId, CancellationToken.None);

        Assert.Equal(0, storage.UploadCount);
        Assert.Empty(storage.Objects);
        Assert.Equal(GalleryExportStatus.Cancelled,
            (await dbContext.GalleryExports.SingleAsync(x => x.Id == exportId)).Status);
    }

    private static OrtakareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrtakareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrtakareDbContext(options);
    }

    private static Event CreateEvent(Guid eventId, Guid ownerId) => new()
    {
        Id = eventId,
        OwnerUserId = ownerId,
        Title = "Test Event",
        EventDateUtc = new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc),
        GalleryToken = Guid.NewGuid().ToString("N"),
        UploadsEnabled = true,
        CreatedAtUtc = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc)
    };

    private static GalleryExport CreateExport(Guid exportId, Guid eventId, GalleryExportStatus status) => new()
    {
        Id = exportId,
        EventId = eventId,
        Status = status,
        PhotoCount = 3,
        CreatedAtUtc = new DateTime(2026, 7, 15, 17, 0, 0, DateTimeKind.Utc)
    };

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
