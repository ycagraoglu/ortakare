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

        modelBuilder.Entity("Ortakare.Api.Features.Users.User", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid");

            entity.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("timestamp with time zone");

            entity.Property<string>("DisplayName")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            entity.Property<string>("Email")
                .IsRequired()
                .HasMaxLength(320)
                .HasColumnType("character varying(320)");

            entity.Property<string>("NormalizedEmail")
                .IsRequired()
                .HasMaxLength(320)
                .HasColumnType("character varying(320)");

            entity.Property<string>("PasswordHash")
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("character varying(500)");

            entity.HasKey("Id");

            entity.HasIndex("NormalizedEmail")
                .IsUnique();

            entity.ToTable("Users");
        });
    }
}
