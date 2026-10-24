using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MealReminderEnabled",
                table: "Users",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MealReminderHour",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WeightReminderEnabled",
                table: "Users",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeightReminderHour",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WorkoutReminderEnabled",
                table: "Users",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkoutReminderHour",
                table: "Users",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MealReminderEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MealReminderHour",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WeightReminderEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WeightReminderHour",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WorkoutReminderEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WorkoutReminderHour",
                table: "Users");
        }
    }
}
