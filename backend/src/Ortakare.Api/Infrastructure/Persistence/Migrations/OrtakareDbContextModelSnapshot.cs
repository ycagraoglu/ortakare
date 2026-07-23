using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Ortakare.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OrtakareDbContext))]
public partial class OrtakareDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0");

        modelBuilder.Entity("Ortakare.Api.Features.Auth.RefreshTokens.RefreshToken", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            entity.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<DateTime>("ExpiresAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<Guid?>("ReplacedByTokenId").HasColumnType("uuid");
            entity.Property<DateTime?>("RevokedAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<string>("TokenHash").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            entity.Property<DateTime?>("UsedAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<Guid>("UserId").HasColumnType("uuid");
            entity.HasKey("Id");
            entity.HasIndex("TokenHash").IsUnique();
            entity.HasIndex("UserId", "ExpiresAtUtc");
            entity.ToTable("RefreshTokens");
        });

        modelBuilder.Entity("Ortakare.Api.Features.Events.Event", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            entity.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<DateTime>("EventDateUtc").HasColumnType("timestamp with time zone");
            entity.Property<string>("GalleryToken").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            entity.Property<Guid>("OwnerUserId").HasColumnType("uuid");
            entity.Property<string>("Title").IsRequired().HasMaxLength(150).HasColumnType("character varying(150)");
            entity.Property<bool>("UploadsEnabled").HasColumnType("boolean");
            entity.Property<DateTime?>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            entity.HasKey("Id");
            entity.HasIndex("GalleryToken").IsUnique();
            entity.HasIndex("OwnerUserId", "EventDateUtc");
            entity.ToTable("Events");
        });

        modelBuilder.Entity("Ortakare.Api.Features.GalleryExports.GalleryExport", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            entity.Property<DateTime?>("CancelledAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<DateTime?>("CompletedAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<Guid>("EventId").HasColumnType("uuid");
            entity.Property<DateTime?>("ExpiresAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<DateTime?>("FailedAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<int>("PhotoCount").HasColumnType("integer");
            entity.Property<string>("Status").IsRequired().HasMaxLength(30).HasColumnType("character varying(30)");
            entity.Property<string>("StorageKey").HasMaxLength(500).HasColumnType("character varying(500)");
            entity.HasKey("Id");
            entity.HasIndex("EventId").IsUnique().HasDatabaseName("UX_GalleryExports_EventId_Active").HasFilter("\"Status\" IN ('Pending', 'Processing')");
            entity.HasIndex("EventId", "CreatedAtUtc");
            entity.HasIndex("Status", "ExpiresAtUtc");
            entity.ToTable("GalleryExports");
        });

        modelBuilder.Entity("Ortakare.Api.Infrastructure.Outbox.OutboxMessage", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            entity.Property<string>("LastError").HasMaxLength(2000).HasColumnType("character varying(2000)");
            entity.Property<Guid?>("LockId").HasColumnType("uuid");
            entity.Property<DateTime?>("LockedAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<DateTime?>("NextAttemptAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<DateTime>("OccurredAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<string>("PayloadJson").IsRequired().HasColumnType("jsonb");
            entity.Property<DateTime?>("ProcessedAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<int>("RetryCount").HasColumnType("integer");
            entity.Property<string>("Type").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            entity.HasKey("Id");
            entity.HasIndex("LockId");
            entity.HasIndex("ProcessedAtUtc", "NextAttemptAtUtc", "LockedAtUtc", "OccurredAtUtc");
            entity.ToTable("OutboxMessages");
        });

        modelBuilder.Entity("Ortakare.Api.Features.Participants.EventGuestParticipant", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            entity.Property<DateTime?>("BlockedAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<string>("DisplayName").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
            entity.Property<Guid>("EventId").HasColumnType("uuid");
            entity.Property<bool>("IsBlocked").HasColumnType("boolean");
            entity.Property<string>("TokenHash").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            entity.HasKey("Id");
            entity.HasIndex("EventId", "CreatedAtUtc");
            entity.HasIndex("TokenHash").IsUnique();
            entity.ToTable("EventGuestParticipants");
        });

        modelBuilder.Entity("Ortakare.Api.Features.Photos.EventGuestPhoto", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            entity.Property<Guid>("ClientUploadId").HasColumnType("uuid");
            entity.Property<string>("ContentType").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            entity.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<Guid>("EventId").HasColumnType("uuid");
            entity.Property<long>("FileSizeBytes").HasColumnType("bigint");
            entity.Property<string>("OriginalFileName").IsRequired().HasMaxLength(255).HasColumnType("character varying(255)");
            entity.Property<Guid>("ParticipantId").HasColumnType("uuid");
            entity.Property<string>("StorageKey").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)");
            entity.HasKey("Id");
            entity.HasIndex("EventId", "CreatedAtUtc");
            entity.HasIndex("ParticipantId", "ClientUploadId").IsUnique();
            entity.ToTable("EventGuestPhotos");
        });

        modelBuilder.Entity("Ortakare.Api.Features.Users.User", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            entity.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<string>("DisplayName").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            entity.Property<string>("Email").IsRequired().HasMaxLength(320).HasColumnType("character varying(320)");
            entity.Property<string>("NormalizedEmail").IsRequired().HasMaxLength(320).HasColumnType("character varying(320)");
            entity.Property<string>("PasswordHash").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)");
            entity.HasKey("Id");
            entity.HasIndex("NormalizedEmail").IsUnique();
            entity.ToTable("Users");
        });

        modelBuilder.Entity("Ortakare.Api.Features.Events.Event", entity =>
        {
            entity.HasOne("Ortakare.Api.Features.Users.User", null).WithMany().HasForeignKey("OwnerUserId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });

        modelBuilder.Entity("Ortakare.Api.Features.GalleryExports.GalleryExport", entity =>
        {
            entity.HasOne("Ortakare.Api.Features.Events.Event", null).WithMany().HasForeignKey("EventId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });

        modelBuilder.Entity("Ortakare.Api.Features.Participants.EventGuestParticipant", entity =>
        {
            entity.HasOne("Ortakare.Api.Features.Events.Event", null).WithMany().HasForeignKey("EventId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });

        modelBuilder.Entity("Ortakare.Api.Features.Photos.EventGuestPhoto", entity =>
        {
            entity.HasOne("Ortakare.Api.Features.Events.Event", null).WithMany().HasForeignKey("EventId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            entity.HasOne("Ortakare.Api.Features.Participants.EventGuestParticipant", null).WithMany().HasForeignKey("ParticipantId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });
    }
}