using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ortakare.Api.Infrastructure.Persistence.Migrations;

public partial class OptimizePostgreSqlIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Events_OwnerUserId_EventDateUtc",
            table: "Events");

        migrationBuilder.DropIndex(
            name: "IX_EventGuestPhotos_EventId_CreatedAtUtc",
            table: "EventGuestPhotos");

        migrationBuilder.DropIndex(
            name: "IX_GalleryExports_EventId_CreatedAtUtc",
            table: "GalleryExports");

        migrationBuilder.DropIndex(
            name: "IX_Notifications_OwnerUserId_DeletedAtUtc_ReadAtUtc_CreatedAtUtc",
            table: "Notifications");

        migrationBuilder.DropIndex(
            name: "IX_Notifications_EventId_DeletedAtUtc_CreatedAtUtc",
            table: "Notifications");

        migrationBuilder.CreateIndex(
            name: "IX_Events_OwnerUserId_EventDateUtc_CreatedAtUtc_Id",
            table: "Events",
            columns: new[] { "OwnerUserId", "EventDateUtc", "CreatedAtUtc", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_EventGuestPhotos_EventId_CreatedAtUtc_Id",
            table: "EventGuestPhotos",
            columns: new[] { "EventId", "CreatedAtUtc", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_EventGuestPhotos_StorageKey",
            table: "EventGuestPhotos",
            column: "StorageKey");

        migrationBuilder.CreateIndex(
            name: "IX_GalleryExports_EventId_CreatedAtUtc_Id",
            table: "GalleryExports",
            columns: new[] { "EventId", "CreatedAtUtc", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_GalleryExports_StorageKey",
            table: "GalleryExports",
            column: "StorageKey",
            filter: "\"StorageKey\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_OwnerUserId_CreatedAtUtc_Id_Active",
            table: "Notifications",
            columns: new[] { "OwnerUserId", "CreatedAtUtc", "Id" },
            filter: "\"DeletedAtUtc\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_OwnerUserId_Unread",
            table: "Notifications",
            column: "OwnerUserId",
            filter: "\"DeletedAtUtc\" IS NULL AND \"ReadAtUtc\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_EventId_CreatedAtUtc_Id_Active",
            table: "Notifications",
            columns: new[] { "EventId", "CreatedAtUtc", "Id" },
            filter: "\"DeletedAtUtc\" IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Events_OwnerUserId_EventDateUtc_CreatedAtUtc_Id",
            table: "Events");

        migrationBuilder.DropIndex(
            name: "IX_EventGuestPhotos_EventId_CreatedAtUtc_Id",
            table: "EventGuestPhotos");

        migrationBuilder.DropIndex(
            name: "IX_EventGuestPhotos_StorageKey",
            table: "EventGuestPhotos");

        migrationBuilder.DropIndex(
            name: "IX_GalleryExports_EventId_CreatedAtUtc_Id",
            table: "GalleryExports");

        migrationBuilder.DropIndex(
            name: "IX_GalleryExports_StorageKey",
            table: "GalleryExports");

        migrationBuilder.DropIndex(
            name: "IX_Notifications_OwnerUserId_CreatedAtUtc_Id_Active",
            table: "Notifications");

        migrationBuilder.DropIndex(
            name: "IX_Notifications_OwnerUserId_Unread",
            table: "Notifications");

        migrationBuilder.DropIndex(
            name: "IX_Notifications_EventId_CreatedAtUtc_Id_Active",
            table: "Notifications");

        migrationBuilder.CreateIndex(
            name: "IX_Events_OwnerUserId_EventDateUtc",
            table: "Events",
            columns: new[] { "OwnerUserId", "EventDateUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_EventGuestPhotos_EventId_CreatedAtUtc",
            table: "EventGuestPhotos",
            columns: new[] { "EventId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_GalleryExports_EventId_CreatedAtUtc",
            table: "GalleryExports",
            columns: new[] { "EventId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_OwnerUserId_DeletedAtUtc_ReadAtUtc_CreatedAtUtc",
            table: "Notifications",
            columns: new[] { "OwnerUserId", "DeletedAtUtc", "ReadAtUtc", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_EventId_DeletedAtUtc_CreatedAtUtc",
            table: "Notifications",
            columns: new[] { "EventId", "DeletedAtUtc", "CreatedAtUtc" });
    }
}