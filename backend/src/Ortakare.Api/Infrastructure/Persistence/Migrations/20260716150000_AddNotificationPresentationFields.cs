using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ortakare.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OrtakareDbContext))]
[Migration("20260716150000_AddNotificationPresentationFields")]
public partial class AddNotificationPresentationFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ActionUrl",
            table: "Notifications",
            type: "character varying(300)",
            maxLength: 300,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Severity",
            table: "Notifications",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "Info");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ActionUrl", table: "Notifications");
        migrationBuilder.DropColumn(name: "Severity", table: "Notifications");
    }
}