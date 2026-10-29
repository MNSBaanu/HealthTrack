using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthTrack.Migrations
{
    /// <inheritdoc />
    public partial class HardenAdminOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TemporaryPasswordExpiresAtUtc",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdminBroadcastId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdminBroadcasts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecipientCount = table.Column<int>(type: "int", nullable: false),
                    RecipientDescription = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminBroadcasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationEmails",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LeaseUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationEmails", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_NotificationEmails_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "NotificationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_AdminBroadcastId",
                table: "Notifications",
                column: "AdminBroadcastId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminBroadcasts_CreatedAtUtc",
                table: "AdminBroadcasts",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationEmails_SentAtUtc_CancelledAtUtc_DueAtUtc",
                table: "NotificationEmails",
                columns: new[] { "SentAtUtc", "CancelledAtUtc", "DueAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AdminBroadcasts_AdminBroadcastId",
                table: "Notifications",
                column: "AdminBroadcastId",
                principalTable: "AdminBroadcasts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AdminBroadcasts_AdminBroadcastId",
                table: "Notifications");

            migrationBuilder.DropTable(
                name: "AdminBroadcasts");

            migrationBuilder.DropTable(
                name: "NotificationEmails");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_AdminBroadcastId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TemporaryPasswordExpiresAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AdminBroadcastId",
                table: "Notifications");
        }
    }
}
