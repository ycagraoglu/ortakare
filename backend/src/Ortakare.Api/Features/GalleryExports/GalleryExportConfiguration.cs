using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ortakare.Api.Features.Events;

namespace Ortakare.Api.Features.GalleryExports;

public sealed class GalleryExportConfiguration : IEntityTypeConfiguration<GalleryExport>
{
    public void Configure(EntityTypeBuilder<GalleryExport> builder)
    {
        builder.ToTable("GalleryExports");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(500);

        builder.HasIndex(x => new { x.EventId, x.CreatedAtUtc });

        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
