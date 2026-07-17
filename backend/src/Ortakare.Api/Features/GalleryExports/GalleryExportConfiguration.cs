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

        builder.HasIndex(x => new { x.EventId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.Status, x.ExpiresAtUtc });

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
