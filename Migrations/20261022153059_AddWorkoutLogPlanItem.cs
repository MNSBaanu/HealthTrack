using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutLogPlanItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkoutPlanItemId",
                table: "WorkoutLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutLogs_WorkoutPlanItemId",
                table: "WorkoutLogs",
                column: "WorkoutPlanItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutLogs_WorkoutPlanItems_WorkoutPlanItemId",
                table: "WorkoutLogs",
                column: "WorkoutPlanItemId",
                principalTable: "WorkoutPlanItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutLogs_WorkoutPlanItems_WorkoutPlanItemId",
                table: "WorkoutLogs");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutLogs_WorkoutPlanItemId",
                table: "WorkoutLogs");

            migrationBuilder.DropColumn(
                name: "WorkoutPlanItemId",
                table: "WorkoutLogs");
        }
    }
}
