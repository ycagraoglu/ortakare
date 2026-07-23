using Microsoft.Extensions.DependencyInjection;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Infrastructure.BackgroundJobs;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.BackgroundJobs;

public sealed class CleanupOrphanFilesJobTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public CleanupOrphanFilesJobTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExecuteAsync_deletes_only_old_unreferenced_managed_objects(
        CancellationToken cancellationToken)
    {
        _factory.ObjectStorage.Reset();
        var now = DateTime.UtcNow;
        var referencedKey = $"events/{Guid.NewGuid():N}/participants/{Guid.NewGuid():N}/{Guid.NewGuid():N}";
        var orphanKey = $"events/{Guid.NewGuid():N}/participants/{Guid.NewGuid():N}/{Guid.NewGuid():N}";
        var recentKey = $"exports/{Guid.NewGuid():N}/{Guid.NewGuid():N}.zip";
        var unmanagedKey = $"other/{Guid.NewGuid():N}";

        _factory.ObjectStorage.Seed(referencedKey, now.AddDays(-3));
        _factory.ObjectStorage.Seed(orphanKey, now.AddDays(-3));
        _factory.ObjectStorage.Seed(recentKey, now.AddHours(-1));
        _factory.ObjectStorage.Seed(unmanagedKey, now.AddDays(-3));

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();
        dbContext.EventGuestPhotos.Add(new EventGuestPhoto
        {
            Id = Guid.CreateVersion7(),
            EventId = Guid.CreateVersion7(),
            ParticipantId = Guid.CreateVersion7(),
            ClientUploadId = Guid.NewGuid(),
            StorageKey = referencedKey,
            OriginalFileName = "photo.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 3,
            CreatedAtUtc = now.AddDays(-3)
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var job = scope.ServiceProvider.GetRequiredService<CleanupOrphanFilesJob>();
        var result = await job.ExecuteAsync(cancellationToken);

        Assert.False(_factory.ObjectStorage.Objects.ContainsKey(orphanKey));
        Assert.True(_factory.ObjectStorage.Objects.ContainsKey(referencedKey));
        Assert.True(_factory.ObjectStorage.Objects.ContainsKey(recentKey));
        Assert.True(_factory.ObjectStorage.Objects.ContainsKey(unmanagedKey));
        Assert.Equal(1, result.DeletedCount);
        Assert.Equal(1, result.OrphanCount);
    }

    [Fact]
    public async Task ExecuteAsync_keeps_object_when_storage_delete_fails(
        CancellationToken cancellationToken)
    {
        _factory.ObjectStorage.Reset();
        var orphanKey = $"exports/{Guid.NewGuid():N}/{Guid.NewGuid():N}.zip";
        _factory.ObjectStorage.Seed(orphanKey, DateTime.UtcNow.AddDays(-3));
        _factory.ObjectStorage.ThrowOnDelete = true;

        using var scope = _factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<CleanupOrphanFilesJob>();
        var result = await job.ExecuteAsync(cancellationToken);

        Assert.True(_factory.ObjectStorage.Objects.ContainsKey(orphanKey));
        Assert.Equal(0, result.DeletedCount);
        Assert.Equal(1, result.FailedCount);
    }
}