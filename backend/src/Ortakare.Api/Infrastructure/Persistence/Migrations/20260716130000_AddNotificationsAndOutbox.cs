using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ortakare.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OrtakareDbContext))]
[Migration("20260716130000_AddNotificationsAndOutbox")]
public partial class AddNotificationsAndOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                RetryCount = table.Column<int>(type: "integer", nullable: false),
                NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_OutboxMessages", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: true),
                Type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                DataJson = table.Column<string>(type: "jsonb", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifications", x => x.Id);
                table.ForeignKey(
                    name: "FK_Notifications_Events_EventId",
                    column: x => x.EventId,
                    principalTable: "Events",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Notifications_Users_OwnerUserId",
                    column: x => x.OwnerUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_EventId_CreatedAtUtc",
            table: "Notifications",
            columns: new[] { "EventId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_OwnerUserId_ReadAtUtc_CreatedAtUtc",
            table: "Notifications",
            columns: new[] { "OwnerUserId", "ReadAtUtc", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_ProcessedAtUtc_NextAttemptAtUtc_OccurredAtUtc",
            table: "OutboxMessages",
            columns: new[] { "ProcessedAtUtc", "NextAttemptAtUtc", "OccurredAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Notifications");
        migrationBuilder.DropTable(name: "OutboxMessages");
    }
}
