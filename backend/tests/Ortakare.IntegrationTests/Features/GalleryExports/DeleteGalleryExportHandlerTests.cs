using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Features.GalleryExports.DeleteGalleryExport;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.GalleryExports;

public sealed class DeleteGalleryExportHandlerTests
{
    [Fact]
    public async Task HandleAsync_DeletesCompletedExportAndStorageObject()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var exportId = Guid.CreateVersion7();
        const string storageKey = "exports/event/export.zip";
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.GalleryExports.Add(CreateExport(exportId, eventId, GalleryExportStatus.Completed, storageKey));
        await dbContext.SaveChangesAsync();

        var storage = new TestObjectStorageService();
        await storage.UploadAsync(
            storageKey,
            new MemoryStream([1, 2, 3]),
            "application/zip",
            3,
            CancellationToken.None);
        var handler = CreateHandler(dbContext, ownerUserId, storage);

        var result = await handler.HandleAsync(eventId, exportId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.StorageObjectDeleted);
        Assert.Equal(GalleryExportStatus.Completed, result.Data.Status);
        Assert.False(storage.Objects.ContainsKey(storageKey));
        Assert.False(await dbContext.GalleryExports.AnyAsync(x => x.Id == exportId));
    }

    [Fact]
    public async Task HandleAsync_DeletesFailedExportWithoutStorageCall()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var exportId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.GalleryExports.Add(CreateExport(exportId, eventId, GalleryExportStatus.Failed));
        await dbContext.SaveChangesAsync();

        var storage = new TestObjectStorageService();
        var handler = CreateHandler(dbContext, ownerUserId, storage);

        var result = await handler.HandleAsync(eventId, exportId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.StorageObjectDeleted);
        Assert.Equal(0, storage.DeleteCount);
        Assert.False(await dbContext.GalleryExports.AnyAsync(x => x.Id == exportId));
    }

    [Theory]
    [InlineData(GalleryExportStatus.Pending)]
    [InlineData(GalleryExportStatus.Processing)]
    public async Task HandleAsync_ReturnsConflictForActiveExport(GalleryExportStatus status)
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var exportId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.GalleryExports.Add(CreateExport(exportId, eventId, status));
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext, ownerUserId, new TestObjectStorageService());

        var result = await handler.HandleAsync(eventId, exportId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
        Assert.True(await dbContext.GalleryExports.AnyAsync(x => x.Id == exportId));
    }

    [Fact]
    public async Task HandleAsync_PreservesDatabaseRecordWhenStorageDeleteFails()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var exportId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.GalleryExports.Add(CreateExport(
            exportId,
            eventId,
            GalleryExportStatus.Completed,
            "exports/event/export.zip"));
        await dbContext.SaveChangesAsync();

        var storage = new TestObjectStorageService { ThrowOnDelete = true };
        var handler = CreateHandler(dbContext, ownerUserId, storage);

        var result = await handler.HandleAsync(eventId, exportId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(503, result.StatusCode);
        Assert.True(await dbContext.GalleryExports.AnyAsync(x => x.Id == exportId));
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFoundForAnotherOwner()
    {
        var eventId = Guid.CreateVersion7();
        var exportId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, Guid.CreateVersion7()));
        dbContext.GalleryExports.Add(CreateExport(exportId, eventId, GalleryExportStatus.Failed));
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(
            dbContext,
            Guid.CreateVersion7(),
            new TestObjectStorageService());

        var result = await handler.HandleAsync(eventId, exportId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.True(await dbContext.GalleryExports.AnyAsync(x => x.Id == exportId));
    }

    private static DeleteGalleryExportHandler CreateHandler(
        OrtakareDbContext dbContext,
        Guid ownerUserId,
        TestObjectStorageService storage) =>
        new(
            dbContext,
            new TestCurrentUser(ownerUserId),
            storage,
            NullLogger<DeleteGalleryExportHandler>.Instance);

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

    private static GalleryExport CreateExport(
        Guid exportId,
        Guid eventId,
        GalleryExportStatus status,
        string? storageKey = null) => new()
    {
        Id = exportId,
        EventId = eventId,
        Status = status,
        PhotoCount = 3,
        StorageKey = storageKey,
        CreatedAtUtc = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc),
        CompletedAtUtc = status == GalleryExportStatus.Completed
            ? new DateTime(2026, 7, 15, 10, 5, 0, DateTimeKind.Utc)
            : null,
        FailedAtUtc = status == GalleryExportStatus.Failed
            ? new DateTime(2026, 7, 15, 10, 5, 0, DateTimeKind.Utc)
            : null
    };

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }
}
