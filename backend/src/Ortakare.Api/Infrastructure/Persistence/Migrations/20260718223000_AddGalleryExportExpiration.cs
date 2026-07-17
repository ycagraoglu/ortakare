using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ortakare.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OrtakareDbContext))]
[Migration("20260718223000_AddGalleryExportExpiration")]
public partial class AddGalleryExportExpiration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "ExpiresAtUtc",
            table: "GalleryExports",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE "GalleryExports"
            SET "ExpiresAtUtc" = "CompletedAtUtc" + INTERVAL '7 days'
            WHERE "Status" = 'Completed'
              AND "CompletedAtUtc" IS NOT NULL
              AND "ExpiresAtUtc" IS NULL;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_GalleryExports_Status_ExpiresAtUtc",
            table: "GalleryExports",
            columns: new[] { "Status", "ExpiresAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_GalleryExports_Status_ExpiresAtUtc",
            table: "GalleryExports");

        migrationBuilder.DropColumn(
            name: "ExpiresAtUtc",
            table: "GalleryExports");
    }
}
