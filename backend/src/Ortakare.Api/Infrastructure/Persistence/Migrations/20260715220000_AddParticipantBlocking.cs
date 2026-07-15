using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ortakare.Api.Infrastructure.Persistence.Migrations;

public partial class AddParticipantBlocking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "BlockedAtUtc",
            table: "EventGuestParticipants",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsBlocked",
            table: "EventGuestParticipants",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BlockedAtUtc",
            table: "EventGuestParticipants");

        migrationBuilder.DropColumn(
            name: "IsBlocked",
            table: "EventGuestParticipants");
    }
}