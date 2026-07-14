using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ortakare.Api.Features.Events;

namespace Ortakare.Api.Features.Participants;

public sealed class EventGuestParticipantConfiguration
    : IEntityTypeConfiguration<EventGuestParticipant>
{
    public void Configure(EntityTypeBuilder<EventGuestParticipant> builder)
    {
        builder.ToTable("EventGuestParticipants");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DisplayName)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.HasIndex(x => new { x.EventId, x.CreatedAtUtc });

        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
