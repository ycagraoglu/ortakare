using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Features.Storage;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.Features.Storage;

public sealed class StorageUploadPolicyServiceTests
{
    [Fact]
    public async Task EvaluateAsync_AllowsUploadWithinQuota()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.EventGuestPhotos.Add(CreatePhoto(eventId, 40));
        await dbContext.SaveChangesAsync();

        var service = new StorageUploadPolicyService(dbContext, CreateConfiguration(quotaBytes: 100));

        var result = await service.EvaluateAsync(
            ownerUserId,
            uploadsEnabled: true,
            fileCount: 1,
            totalBytes: 50,
            largestFileBytes: 50,
            CancellationToken.None);

        Assert.True(result.CanUpload);
        Assert.Equal(40, result.CurrentUsageBytes);
        Assert.Equal(90, result.ProjectedUsageBytes);
        Assert.Equal("Critical", result.QuotaStatus);
    }

    [Fact]
    public async Task EvaluateAsync_RejectsUploadThatWouldExceedQuota()
    {
        var ownerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.Add(CreateEvent(eventId, ownerUserId));
        dbContext.EventGuestPhotos.Add(CreatePhoto(eventId, 80));
        await dbContext.SaveChangesAsync();

        var service = new StorageUploadPolicyService(dbContext, CreateConfiguration(quotaBytes: 100));

        var result = await service.EvaluateAsync(
            ownerUserId,
            uploadsEnabled: true,
            fileCount: 1,
            totalBytes: 30,
            largestFileBytes: 30,
            CancellationToken.None);

        Assert.False(result.CanUpload);
        Assert.Equal("Exceeded", result.QuotaStatus);
        Assert.Equal(110, result.ProjectedUsageBytes);
        Assert.Equal("Seçilen dosyalar depolama kotasını aşacaktır.", result.Message);
    }

    [Fact]
    public async Task EvaluateAsync_ExcludesAnotherOwnersUsage()
    {
        var ownerUserId = Guid.CreateVersion7();
        var anotherOwnerUserId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var anotherEventId = Guid.CreateVersion7();
        await using var dbContext = CreateDbContext();
        dbContext.Events.AddRange(
            CreateEvent(eventId, ownerUserId),
            CreateEvent(anotherEventId, anotherOwnerUserId));
        dbContext.EventGuestPhotos.AddRange(
            CreatePhoto(eventId, 20),
            CreatePhoto(anotherEventId, 90));
        await dbContext.SaveChangesAsync();

        var service = new StorageUploadPolicyService(dbContext, CreateConfiguration(quotaBytes: 100));

        var result = await service.EvaluateAsync(
            ownerUserId,
            uploadsEnabled: true,
            fileCount: 1,
            totalBytes: 30,
            largestFileBytes: 30,
            CancellationToken.None);

        Assert.True(result.CanUpload);
        Assert.Equal(20, result.CurrentUsageBytes);
        Assert.Equal(50, result.ProjectedUsageBytes);
    }

    [Fact]
    public async Task EvaluateAsync_RejectsUploadWhenEventIsClosed()
    {
        await using var dbContext = CreateDbContext();
        var service = new StorageUploadPolicyService(dbContext, CreateConfiguration(quotaBytes: 100));

        var result = await service.EvaluateAsync(
            Guid.CreateVersion7(),
            uploadsEnabled: false,
            fileCount: 1,
            totalBytes: 10,
            largestFileBytes: 10,
            CancellationToken.None);

        Assert.False(result.CanUpload);
        Assert.Equal("Bu albüm yeni yüklemelere kapatıldı.", result.Message);
    }

    private static OrtakareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrtakareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrtakareDbContext(options);
    }

    private static IConfiguration CreateConfiguration(long quotaBytes)
    {
        var values = new Dictionary<string, string?>
        {
            ["OwnerStorageQuota:QuotaBytes"] = quotaBytes.ToString(),
            ["OwnerStorageQuota:WarningThresholdPercent"] = "80",
            ["OwnerStorageQuota:CriticalThresholdPercent"] = "90",
            ["PhotoUpload:MaxFileSizeBytes"] = "100"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static Event CreateEvent(Guid eventId, Guid ownerUserId) => new()
    {
        Id = eventId,
        OwnerUserId = ownerUserId,
        Title = "Quota Event",
        EventDateUtc = DateTime.UtcNow,
        GalleryToken = Guid.NewGuid().ToString("N"),
        UploadsEnabled = true,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static EventGuestPhoto CreatePhoto(Guid eventId, long fileSizeBytes) => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = eventId,
        ParticipantId = Guid.CreateVersion7(),
        ClientUploadId = Guid.CreateVersion7(),
        StorageKey = Guid.NewGuid().ToString("N"),
        OriginalFileName = "photo.jpg",
        ContentType = "image/jpeg",
        FileSizeBytes = fileSizeBytes,
        CreatedAtUtc = DateTime.UtcNow
    };
}