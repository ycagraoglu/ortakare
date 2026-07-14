using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ortakare.Api.Features.Events;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.GalleryToken)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.GalleryToken)
            .IsUnique();

        builder.HasIndex(x => new { x.OwnerUserId, x.EventDateUtc });
    }
}
