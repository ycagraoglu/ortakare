using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Participants;

namespace Ortakare.Api.Features.Photos;

public sealed class EventGuestPhotoConfiguration : IEntityTypeConfiguration<EventGuestPhoto>
{
    public void Configure(EntityTypeBuilder<EventGuestPhoto> builder)
    {
        builder.ToTable("EventGuestPhotos");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => new { x.ParticipantId, x.ClientUploadId }).IsUnique();
        builder.HasIndex(x => new { x.EventId, x.CreatedAtUtc, x.Id })
            .HasDatabaseName("IX_EventGuestPhotos_EventId_CreatedAtUtc_Id");
        builder.HasIndex(x => x.StorageKey)
            .HasDatabaseName("IX_EventGuestPhotos_StorageKey");

        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<EventGuestParticipant>()
            .WithMany()
            .HasForeignKey(x => x.ParticipantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}