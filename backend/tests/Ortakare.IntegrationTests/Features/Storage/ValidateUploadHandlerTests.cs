using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Features.Storage.ValidateUpload;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Storage;

public sealed class ValidateUploadHandlerTests
{
    [Fact]
    public async Task HandleAsync_AllowsUploadWithinLimits()
    {
        var ownerId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        await using var db = CreateDbContext();
        db.Events.Add(CreateEvent(eventId, ownerId, true));
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, ownerId, quotaBytes: 1_000, maxFileSizeBytes: 500);
        var result = await handler.HandleAsync(
            new ValidateUploadRequest(eventId, 2, 400, 250),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.CanUpload);
        Assert.Equal(400, result.Data.ProjectedUsageBytes);
        Assert.Equal(40m, result.Data.ProjectedUsagePercent);
        Assert.Equal("Healthy", result.Data.QuotaStatus);
    }

    [Fact]
    public async Task HandleAsync_RejectsUploadThatWouldExceedQuota()
    {
        var ownerId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        await using var db = CreateDbContext();
        db.Events.Add(CreateEvent(eventId, ownerId, true));
        db.EventGuestPhotos.Add(CreatePhoto(eventId, participantId, 800));
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, ownerId, quotaBytes: 1_000, maxFileSizeBytes: 500);
        var result = await handler.HandleAsync(
            new ValidateUploadRequest(eventId, 1, 300, 300),
            CancellationToken.None);

        Assert.False(result.Data!.CanUpload);
        Assert.Equal("Exceeded", result.Data.QuotaStatus);
        Assert.Equal(1_100, result.Data.ProjectedUsageBytes);
    }

    [Fact]
    public async Task HandleAsync_RejectsLargestFileAboveLimit()
    {
        var ownerId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        await using var db = CreateDbContext();
        db.Events.Add(CreateEvent(eventId, ownerId, true));
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, ownerId, quotaBytes: 10_000, maxFileSizeBytes: 500);
        var result = await handler.HandleAsync(
            new ValidateUploadRequest(eventId, 2, 900, 600),
            CancellationToken.None);

        Assert.False(result.Data!.CanUpload);
        Assert.Contains("dosya boyutunu", result.Data.Message);
    }

    [Fact]
    public async Task HandleAsync_RejectsClosedEvent()
    {
        var ownerId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        await using var db = CreateDbContext();
        db.Events.Add(CreateEvent(eventId, ownerId, false));
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, ownerId, quotaBytes: 10_000, maxFileSizeBytes: 500);
        var result = await handler.HandleAsync(
            new ValidateUploadRequest(eventId, 1, 100, 100),
            CancellationToken.None);

        Assert.False(result.Data!.CanUpload);
        Assert.Contains("kapatıldı", result.Data.Message);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFoundForAnotherOwnersEvent()
    {
        var eventId = Guid.CreateVersion7();
        await using var db = CreateDbContext();
        db.Events.Add(CreateEvent(eventId, Guid.CreateVersion7(), true));
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, Guid.CreateVersion7(), quotaBytes: 10_000, maxFileSizeBytes: 500);
        var result = await handler.HandleAsync(
            new ValidateUploadRequest(eventId, 1, 100, 100),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    private static ValidateUploadHandler CreateHandler(
        OrtakareDbContext db,
        Guid ownerId,
        long quotaBytes,
        long maxFileSizeBytes)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OwnerStorageQuota:QuotaBytes"] = quotaBytes.ToString(),
                ["OwnerStorageQuota:WarningThresholdPercent"] = "80",
                ["OwnerStorageQuota:CriticalThresholdPercent"] = "95",
                ["PhotoUpload:MaxFileSizeBytes"] = maxFileSizeBytes.ToString()
            })
            .Build();

        return new ValidateUploadHandler(db, new TestCurrentUser(ownerId), configuration);
    }

    private static OrtakareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrtakareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrtakareDbContext(options);
    }

    private static Event CreateEvent(Guid id, Guid ownerId, bool uploadsEnabled) => new()
    {
        Id = id,
        OwnerUserId = ownerId,
        Title = "Storage Event",
        EventDateUtc = DateTime.UtcNow.AddDays(1),
        GalleryToken = Guid.NewGuid().ToString("N"),
        UploadsEnabled = uploadsEnabled,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static EventGuestPhoto CreatePhoto(Guid eventId, Guid participantId, long size) => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = eventId,
        ParticipantId = participantId,
        ClientUploadId = Guid.CreateVersion7(),
        StorageKey = Guid.NewGuid().ToString("N"),
        OriginalFileName = "photo.jpg",
        ContentType = "image/jpeg",
        FileSizeBytes = size,
        CreatedAtUtc = DateTime.UtcNow
    };

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }
}