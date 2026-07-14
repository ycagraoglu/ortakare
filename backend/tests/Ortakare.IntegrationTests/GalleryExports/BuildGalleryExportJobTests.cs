using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Photos;
using Ortakare.Api.Features.Users;
using Ortakare.Api.Infrastructure.BackgroundJobs;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.IntegrationTests.GalleryExports;

public sealed class BuildGalleryExportJobTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public BuildGalleryExportJobTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExecuteAsync_creates_zip_and_completes_export(
        CancellationToken cancellationToken)
    {
        _factory.ObjectStorage.Reset();
        var seeded = await SeedExportAsync(cancellationToken);

        using var scope = _factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<BuildGalleryExportJob>();
        await job.ExecuteAsync(seeded.ExportId, cancellationToken);

        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();
        var galleryExport = await dbContext.GalleryExports
            .AsNoTracking()
            .SingleAsync(x => x.Id == seeded.ExportId, cancellationToken);

        Assert.Equal(GalleryExportStatus.Completed, galleryExport.Status);
        Assert.NotNull(galleryExport.CompletedAtUtc);
        Assert.Null(galleryExport.FailedAtUtc);
        Assert.Equal(2, galleryExport.PhotoCount);
        Assert.NotNull(galleryExport.StorageKey);

        var zipObject = _factory.ObjectStorage.Objects[galleryExport.StorageKey];
        Assert.Equal("application/zip", zipObject.ContentType);

        using var zipStream = new MemoryStream(zipObject.Content);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        Assert.Equal(2, archive.Entries.Count);
        Assert.All(archive.Entries, entry => Assert.True(entry.Length > 0));
    }

    [Fact]
    public async Task ExecuteAsync_marks_export_failed_when_photo_read_fails(
        CancellationToken cancellationToken)
    {
        _factory.ObjectStorage.Reset();
        var seeded = await SeedExportAsync(cancellationToken);
        _factory.ObjectStorage.ThrowOnRead = true;

        using var scope = _factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<BuildGalleryExportJob>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => job.ExecuteAsync(seeded.ExportId, cancellationToken));

        var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();
        var galleryExport = await dbContext.GalleryExports
            .AsNoTracking()
            .SingleAsync(x => x.Id == seeded.ExportId, cancellationToken);

        Assert.Equal(GalleryExportStatus.Failed, galleryExport.Status);
        Assert.NotNull(galleryExport.FailedAtUtc);
        Assert.Null(galleryExport.CompletedAtUtc);
        Assert.Null(galleryExport.StorageKey);
    }

    private async Task<SeededExport> SeedExportAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var userId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        var exportId = Guid.CreateVersion7();
        var firstPhotoId = Guid.CreateVersion7();
        var secondPhotoId = Guid.CreateVersion7();
        var firstKey = $"events/{eventId:N}/participants/{participantId:N}/{firstPhotoId:N}";
        var secondKey = $"events/{eventId:N}/participants/{participantId:N}/{secondPhotoId:N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OrtakareDbContext>();

            dbContext.Users.Add(new User
            {
                Id = userId,
                DisplayName = "Export Owner",
                Email = $"export-{Guid.NewGuid():N}@example.com",
                NormalizedEmail = $"EXPORT-{Guid.NewGuid():N}@EXAMPLE.COM",
                PasswordHash = "unused",
                CreatedAtUtc = now
            });

            dbContext.Events.Add(new Event
            {
                Id = eventId,
                OwnerUserId = userId,
                Title = "Export Event",
                EventDateUtc = now.AddDays(1),
                GalleryToken = Guid.NewGuid().ToString("N"),
                UploadsEnabled = false,
                CreatedAtUtc = now
            });

            dbContext.EventGuestParticipants.Add(new EventGuestParticipant
            {
                Id = participantId,
                EventId = eventId,
                DisplayName = "Guest",
                TokenHash = Guid.NewGuid().ToString("N").PadRight(64, '0'),
                CreatedAtUtc = now
            });

            dbContext.EventGuestPhotos.AddRange(
                CreatePhoto(firstPhotoId, eventId, participantId, firstKey, "image/jpeg", now),
                CreatePhoto(secondPhotoId, eventId, participantId, secondKey, "image/png", now.AddSeconds(1)));

            dbContext.GalleryExports.Add(new GalleryExport
            {
                Id = exportId,
                EventId = eventId,
                Status = GalleryExportStatus.Pending,
                PhotoCount = 2,
                CreatedAtUtc = now
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await _factory.ObjectStorage.UploadAsync(
            firstKey,
            new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }),
            "image/jpeg",
            4,
            cancellationToken);
        await _factory.ObjectStorage.UploadAsync(
            secondKey,
            new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 }),
            "image/png",
            4,
            cancellationToken);

        return new SeededExport(exportId);
    }

    private static EventGuestPhoto CreatePhoto(
        Guid id,
        Guid eventId,
        Guid participantId,
        string storageKey,
        string contentType,
        DateTime createdAtUtc) => new()
    {
        Id = id,
        EventId = eventId,
        ParticipantId = participantId,
        ClientUploadId = Guid.NewGuid(),
        StorageKey = storageKey,
        OriginalFileName = $"{id:N}",
        ContentType = contentType,
        FileSizeBytes = 4,
        CreatedAtUtc = createdAtUtc
    };

    private sealed record SeededExport(Guid ExportId);
}