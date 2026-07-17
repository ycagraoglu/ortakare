using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ortakare.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OrtakareDbContext))]
[Migration("20260718213000_PreventDuplicateActiveGalleryExports")]
public partial class PreventDuplicateActiveGalleryExports : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "UX_GalleryExports_EventId_Active",
            table: "GalleryExports",
            column: "EventId",
            unique: true,
            filter: "\"Status\" IN ('Pending', 'Processing')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_GalleryExports_EventId_Active",
            table: "GalleryExports");
    }
}
