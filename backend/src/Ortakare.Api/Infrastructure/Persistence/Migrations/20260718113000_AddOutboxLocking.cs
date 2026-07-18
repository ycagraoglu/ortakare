using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ortakare.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OrtakareDbContext))]
[Migration("20260718113000_AddOutboxLocking")]
public partial class AddOutboxLocking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "LockId",
            table: "OutboxMessages",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LockedAtUtc",
            table: "OutboxMessages",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.DropIndex(
            name: "IX_OutboxMessages_ProcessedAtUtc_NextAttemptAtUtc_OccurredAtUtc",
            table: "OutboxMessages");

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_LockId",
            table: "OutboxMessages",
            column: "LockId");

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_ProcessedAtUtc_NextAttemptAtUtc_LockedAtUtc_OccurredAtUtc",
            table: "OutboxMessages",
            columns: new[] { "ProcessedAtUtc", "NextAttemptAtUtc", "LockedAtUtc", "OccurredAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_OutboxMessages_LockId",
            table: "OutboxMessages");

        migrationBuilder.DropIndex(
            name: "IX_OutboxMessages_ProcessedAtUtc_NextAttemptAtUtc_LockedAtUtc_OccurredAtUtc",
            table: "OutboxMessages");

        migrationBuilder.DropColumn(name: "LockId", table: "OutboxMessages");
        migrationBuilder.DropColumn(name: "LockedAtUtc", table: "OutboxMessages");

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_ProcessedAtUtc_NextAttemptAtUtc_OccurredAtUtc",
            table: "OutboxMessages",
            columns: new[] { "ProcessedAtUtc", "NextAttemptAtUtc", "OccurredAtUtc" });
    }
}