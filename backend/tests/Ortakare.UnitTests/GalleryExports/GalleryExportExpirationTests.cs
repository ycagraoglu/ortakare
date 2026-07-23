using Ortakare.Api.Features.GalleryExports;

namespace Ortakare.UnitTests.GalleryExports;

public sealed class GalleryExportExpirationTests
{
    [Fact]
    public void EnsureExpiration_sets_expiration_seven_days_after_completion()
    {
        var completedAtUtc = new DateTime(2026, 7, 18, 20, 0, 0, DateTimeKind.Utc);
        var galleryExport = new GalleryExport
        {
            Status = GalleryExportStatus.Completed,
            CompletedAtUtc = completedAtUtc
        };

        galleryExport.EnsureExpiration();

        Assert.Equal(completedAtUtc.AddDays(GalleryExport.RetentionDays), galleryExport.ExpiresAtUtc);
    }

    [Fact]
    public void EnsureExpiration_does_not_overwrite_existing_expiration()
    {
        var existingExpiration = new DateTime(2026, 7, 20, 20, 0, 0, DateTimeKind.Utc);
        var galleryExport = new GalleryExport
        {
            Status = GalleryExportStatus.Completed,
            CompletedAtUtc = existingExpiration.AddDays(-1),
            ExpiresAtUtc = existingExpiration
        };

        galleryExport.EnsureExpiration();

        Assert.Equal(existingExpiration, galleryExport.ExpiresAtUtc);
    }

    [Fact]
    public void EnsureExpiration_does_not_expire_incomplete_export()
    {
        var galleryExport = new GalleryExport
        {
            Status = GalleryExportStatus.Processing
        };

        galleryExport.EnsureExpiration();

        Assert.Null(galleryExport.ExpiresAtUtc);
    }
}
