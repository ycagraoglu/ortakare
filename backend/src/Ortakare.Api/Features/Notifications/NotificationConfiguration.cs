using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ortakare.Api.Features.Events;
using Ortakare.Api.Features.Users;

namespace Ortakare.Api.Features.Notifications;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Severity).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ActionUrl).HasMaxLength(300);
        builder.Property(x => x.DataJson).HasColumnType("jsonb");

        builder.HasQueryFilter(x => x.DeletedAtUtc == null);

        builder.HasIndex(x => new { x.OwnerUserId, x.DeletedAtUtc, x.ReadAtUtc, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.EventId, x.DeletedAtUtc, x.CreatedAtUtc });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}