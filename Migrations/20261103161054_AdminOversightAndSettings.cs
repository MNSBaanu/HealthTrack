using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthTrack.Migrations
{
    /// <inheritdoc />
    public partial class AdminOversightAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RatingHidden",
                table: "Appointments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ChatFaqs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Question = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatFaqs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    BannerEnabled = table.Column<bool>(type: "bit", nullable: false),
                    BannerMessage = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TemporaryPasswordHours = table.Column<int>(type: "int", nullable: false),
                    MaxFailedAccessAttempts = table.Column<int>(type: "int", nullable: false),
                    LockoutMinutes = table.Column<int>(type: "int", nullable: false),
                    ImportMaxRows = table.Column<int>(type: "int", nullable: false),
                    WeeklyReportEnabled = table.Column<bool>(type: "bit", nullable: false),
                    WeeklyReportSentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatFaqs");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "RatingHidden",
                table: "Appointments");
        }
    }
}
