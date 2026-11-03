using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthTrack.Migrations
{
    /// <inheritdoc />
    public partial class AdminTwoFactorSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequireAdminTwoFactor",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequireAdminTwoFactor",
                table: "SystemSettings");
        }
    }
}
