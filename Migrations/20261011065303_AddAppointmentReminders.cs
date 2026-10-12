using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppointmentId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AppointmentStartUtc",
                table: "Notifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDismissed",
                table: "Notifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_AppointmentId",
                table: "Notifications",
                column: "AppointmentId",
                unique: true,
                filter: "[AppointmentId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Appointments_AppointmentId",
                table: "Notifications",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Appointments_AppointmentId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_AppointmentId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "AppointmentStartUtc",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "IsDismissed",
                table: "Notifications");
        }
    }
}
