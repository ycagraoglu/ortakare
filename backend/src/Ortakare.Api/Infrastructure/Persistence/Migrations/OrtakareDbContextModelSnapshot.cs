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

        modelBuilder.Entity("Ortakare.Api.Features.Participants.EventGuestParticipant", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            entity.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            entity.Property<string>("DisplayName").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
            entity.Property<Guid>("EventId").HasColumnType("uuid");
            entity.Property<string>("TokenHash").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)");
            entity.HasKey("Id");
            entity.HasIndex("EventId", "CreatedAtUtc");
            entity.HasIndex("TokenHash").IsUnique();
            entity.ToTable("EventGuestParticipants");
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
            entity.HasOne("Ortakare.Api.Features.Users.User", null)
                .WithMany()
                .HasForeignKey("OwnerUserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Ortakare.Api.Features.Participants.EventGuestParticipant", entity =>
        {
            entity.HasOne("Ortakare.Api.Features.Events.Event", null)
                .WithMany()
                .HasForeignKey("EventId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
    }
}
