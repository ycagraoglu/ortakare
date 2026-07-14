using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ortakare.Api.Infrastructure.Persistence.Migrations;

public partial class AddEventGuestPhotos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EventGuestPhotos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                ClientUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventGuestPhotos", x => x.Id);
                table.ForeignKey(
                    name: "FK_EventGuestPhotos_EventGuestParticipants_ParticipantId",
                    column: x => x.ParticipantId,
                    principalTable: "EventGuestParticipants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_EventGuestPhotos_Events_EventId",
                    column: x => x.EventId,
                    principalTable: "Events",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EventGuestPhotos_EventId_CreatedAtUtc",
            table: "EventGuestPhotos",
            columns: new[] { "EventId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_EventGuestPhotos_ParticipantId_ClientUploadId",
            table: "EventGuestPhotos",
            columns: new[] { "ParticipantId", "ClientUploadId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EventGuestPhotos");
    }
}