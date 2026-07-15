using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Features.GalleryExports.RetryFailedGalleryExport;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.GalleryExports;

public sealed class RetryFailedGalleryExportHandlerTests
{
    [Fact]
    public async Task HandleAsync_RequeuesOwnedFailedExport()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var exportId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.GalleryExports.Add(CreateExport(exportId, eventId, GalleryExportStatus.Failed));
        await dbContext.SaveChangesAsync();
        var scheduler = new TestGalleryExportJobScheduler();
        var handler = new RetryFailedGalleryExportHandler(dbContext, new TestCurrentUser(ownerUserId), scheduler);

        var result = await handler.HandleAsync(eventId, exportId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(202, result.StatusCode);
        Assert.Equal(GalleryExportStatus.Pending, result.Data!.Status);
        Assert.Contains(exportId, scheduler.ExportIds);
        var stored = await dbContext.GalleryExports.SingleAsync(x => x.Id == exportId);
        Assert.Equal(GalleryExportStatus.Pending, stored.Status);
        Assert.Null(stored.FailedAtUtc);
        Assert.Null(stored.CompletedAtUtc);
    }

    [Theory]
    [InlineData(GalleryExportStatus.Pending)]
    [InlineData(GalleryExportStatus.Processing)]
    [InlineData(GalleryExportStatus.Completed)]
    public async Task HandleAsync_ReturnsConflictForNonFailedExport(GalleryExportStatus status)
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var exportId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.GalleryExports.Add(CreateExport(exportId, eventId, status));
        await dbContext.SaveChangesAsync();
        var scheduler = new TestGalleryExportJobScheduler();
        var handler = new RetryFailedGalleryExportHandler(dbContext, new TestCurrentUser(ownerUserId), scheduler);

        var result = await handler.HandleAsync(eventId, exportId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
        Assert.Empty(scheduler.ExportIds);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFoundForAnotherOwnersExport()
    {
        var eventId = Guid.CreateVersion7();
        var exportId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, Guid.CreateVersion7()));
        dbContext.GalleryExports.Add(CreateExport(exportId, eventId, GalleryExportStatus.Failed));
        await dbContext.SaveChangesAsync();
        var scheduler = new TestGalleryExportJobScheduler();
        var handler = new RetryFailedGalleryExportHandler(dbContext, new TestCurrentUser(Guid.CreateVersion7()), scheduler);

        var result = await handler.HandleAsync(eventId, exportId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Empty(scheduler.ExportIds);
        Assert.Equal(GalleryExportStatus.Failed, (await dbContext.GalleryExports.SingleAsync()).Status);
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
        PhotoCount = 4,
        StorageKey = status == GalleryExportStatus.Completed ? $"exports/{eventId:N}/{exportId:N}.zip" : null,
        CreatedAtUtc = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc),
        CompletedAtUtc = status == GalleryExportStatus.Completed ? new DateTime(2026, 7, 15, 10, 5, 0, DateTimeKind.Utc) : null,
        FailedAtUtc = status == GalleryExportStatus.Failed ? new DateTime(2026, 7, 15, 10, 5, 0, DateTimeKind.Utc) : null
    };

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }
}
