using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ortakare.Api.Features.EventAudit;

public sealed class EventAuditLogConfiguration : IEntityTypeConfiguration<EventAuditLog>
{
    public void Configure(EntityTypeBuilder<EventAuditLog> builder)
    {
        builder.ToTable("EventAuditLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ActorType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.TargetType).HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb");

        builder.HasIndex(x => new { x.EventId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.OwnerUserId, x.CreatedAtUtc });
    }
}
