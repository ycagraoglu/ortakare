using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Infrastructure.BackgroundJobs;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.GalleryExports;

public sealed class CleanupExpiredGalleryExportsJobTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public CleanupExpiredGalleryExportsJobTests(OrtakareApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Execute_deletes_expired_storage_object_and_database_record(
        CancellationToken cancellationToken)
    {
        _factory.ObjectStorage.Reset();
        var seeded = await SeedCompletedExportAsync(
            expiresAtUtc: DateTime.UtcNow.AddMinutes(-5),
            cancellationToken);
        await StoreExportObjectAsync(seeded.StorageKey, cancellationToken);

        using var scope = _factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<CleanupExpiredGalleryExportsJob>();

        var result = await job.ExecuteAsync(cancellationToken);

        Assert.Equal(1, result.ScannedCount);
        Assert.Equal(1, result.DeletedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.DoesNotContain(seeded.StorageKey, _factory.ObjectStorage.Objects.Keys);

        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();
        Assert.False(await dbContext.GalleryExports.AnyAsync(
            x => x.Id == seeded.ExportId,
            cancellationToken));
    }

    [Fact]
    public async Task Execute_does_not_delete_unexpired_export(
        CancellationToken cancellationToken)
    {
        _factory.ObjectStorage.Reset();
        var seeded = await SeedCompletedExportAsync(
            expiresAtUtc: DateTime.UtcNow.AddHours(1),
            cancellationToken);
        await StoreExportObjectAsync(seeded.StorageKey, cancellationToken);

        using var scope = _factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<CleanupExpiredGalleryExportsJob>();

        var result = await job.ExecuteAsync(cancellationToken);

        Assert.Equal(0, result.ScannedCount);
        Assert.Contains(seeded.StorageKey, _factory.ObjectStorage.Objects.Keys);

        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();
        Assert.True(await dbContext.GalleryExports.AnyAsync(
            x => x.Id == seeded.ExportId,
            cancellationToken));
    }

    [Fact]
    public async Task Execute_keeps_database_record_when_storage_delete_fails(
        CancellationToken cancellationToken)
    {
        _factory.ObjectStorage.Reset();
        var seeded = await SeedCompletedExportAsync(
            expiresAtUtc: DateTime.UtcNow.AddMinutes(-5),
            cancellationToken);
        await StoreExportObjectAsync(seeded.StorageKey, cancellationToken);
        _factory.ObjectStorage.ThrowOnDelete = true;

        using var scope = _factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<CleanupExpiredGalleryExportsJob>();

        var result = await job.ExecuteAsync(cancellationToken);

        Assert.Equal(1, result.ScannedCount);
        Assert.Equal(0, result.DeletedCount);
        Assert.Equal(1, result.FailedCount);

        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();
        Assert.True(await dbContext.GalleryExports.AnyAsync(
            x => x.Id == seeded.ExportId,
            cancellationToken));
    }

    private async Task<SeededExport> SeedCompletedExportAsync(
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        var eventId = Guid.CreateVersion7();
        var exportId = Guid.CreateVersion7();
        var storageKey = $"exports/{eventId:N}/{exportId:N}.zip";
        var now = DateTime.UtcNow;

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();
        dbContext.Events.Add(new Event
        {
            Id = eventId,
            OwnerUserId = Guid.CreateVersion7(),
            Title = "Cleanup Event",
            EventDateUtc = now.AddDays(1),
            GalleryToken = Guid.NewGuid().ToString("N"),
            UploadsEnabled = true,
            CreatedAtUtc = now
        });
        dbContext.GalleryExports.Add(new GalleryExport
        {
            Id = exportId,
            EventId = eventId,
            Status = GalleryExportStatus.Completed,
            PhotoCount = 2,
            StorageKey = storageKey,
            CreatedAtUtc = now.AddDays(-8),
            CompletedAtUtc = now.AddDays(-7),
            ExpiresAtUtc = expiresAtUtc
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SeededExport(exportId, storageKey);
    }

    private async Task StoreExportObjectAsync(
        string storageKey,
        CancellationToken cancellationToken)
    {
        await using var content = new MemoryStream([1, 2, 3, 4]);
        await _factory.ObjectStorage.UploadAsync(
            storageKey,
            content,
            "application/zip",
            content.Length,
            cancellationToken);
    }

    private sealed record SeededExport(Guid ExportId, string StorageKey);
}
