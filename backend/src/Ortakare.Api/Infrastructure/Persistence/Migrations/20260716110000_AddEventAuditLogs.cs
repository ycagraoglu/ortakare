using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ortakare.Api.Infrastructure.Persistence.Migrations;

public partial class AddEventAuditLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EventAuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                ActorType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                TargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                TargetId = table.Column<Guid>(type: "uuid", nullable: true),
                Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_EventAuditLogs", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_EventAuditLogs_EventId_CreatedAtUtc",
            table: "EventAuditLogs",
            columns: new[] { "EventId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_EventAuditLogs_OwnerUserId_CreatedAtUtc",
            table: "EventAuditLogs",
            columns: new[] { "OwnerUserId", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EventAuditLogs");
    }
}
