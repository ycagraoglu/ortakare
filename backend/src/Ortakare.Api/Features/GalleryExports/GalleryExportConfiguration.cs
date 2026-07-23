using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ortakare.Api.Features.Events;

namespace Ortakare.Api.Features.GalleryExports;

public sealed class GalleryExportConfiguration : IEntityTypeConfiguration<GalleryExport>
{
    public const string ActiveExportUniqueIndexName = "UX_GalleryExports_EventId_Active";

    public void Configure(EntityTypeBuilder<GalleryExport> builder)
    {
        builder.ToTable("GalleryExports");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(500);

        builder.HasIndex(x => new { x.EventId, x.CreatedAtUtc, x.Id })
            .HasDatabaseName("IX_GalleryExports_EventId_CreatedAtUtc_Id");
        builder.HasIndex(x => new { x.Status, x.ExpiresAtUtc })
            .HasDatabaseName("IX_GalleryExports_Status_ExpiresAtUtc");
        builder.HasIndex(x => x.StorageKey)
            .HasDatabaseName("IX_GalleryExports_StorageKey")
            .HasFilter("\"StorageKey\" IS NOT NULL");

        builder.HasIndex(x => x.EventId)
            .HasDatabaseName(ActiveExportUniqueIndexName)
            .IsUnique()
            .HasFilter("\"Status\" IN ('Pending', 'Processing')");

        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}