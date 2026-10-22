using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "BodyFatPercent",
                table: "HealthMetrics",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiastolicBp",
                table: "HealthMetrics",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RestingHeartRate",
                table: "HealthMetrics",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SleepHours",
                table: "HealthMetrics",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Steps",
                table: "HealthMetrics",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SystolicBp",
                table: "HealthMetrics",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BodyFatPercent",
                table: "HealthMetrics");

            migrationBuilder.DropColumn(
                name: "DiastolicBp",
                table: "HealthMetrics");

            migrationBuilder.DropColumn(
                name: "RestingHeartRate",
                table: "HealthMetrics");

            migrationBuilder.DropColumn(
                name: "SleepHours",
                table: "HealthMetrics");

            migrationBuilder.DropColumn(
                name: "Steps",
                table: "HealthMetrics");

            migrationBuilder.DropColumn(
                name: "SystolicBp",
                table: "HealthMetrics");
        }
    }
}
