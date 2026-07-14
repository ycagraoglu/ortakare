using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ortakare.Api.Infrastructure.Persistence.Migrations;

public partial class AddEventGuestParticipants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EventGuestParticipants",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                DisplayName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventGuestParticipants", x => x.Id);
                table.ForeignKey(
                    name: "FK_EventGuestParticipants_Events_EventId",
                    column: x => x.EventId,
                    principalTable: "Events",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EventGuestParticipants_EventId_CreatedAtUtc",
            table: "EventGuestParticipants",
            columns: new[] { "EventId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_EventGuestParticipants_TokenHash",
            table: "EventGuestParticipants",
            column: "TokenHash",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EventGuestParticipants");
    }
}
