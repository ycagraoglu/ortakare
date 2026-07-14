using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ortakare.Api.Infrastructure.Persistence.Migrations;

public partial class AddEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                EventDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                GalleryToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                UploadsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Events", x => x.Id);
                table.ForeignKey(
                    name: "FK_Events_Users_OwnerUserId",
                    column: x => x.OwnerUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Events_GalleryToken",
            table: "Events",
            column: "GalleryToken",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Events_OwnerUserId_EventDateUtc",
            table: "Events",
            columns: new[] { "OwnerUserId", "EventDateUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Events");
    }
}
