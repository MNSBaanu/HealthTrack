using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddMealLogPlanLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DietPlanMealId",
                table: "MealLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MealLogs_DietPlanMealId",
                table: "MealLogs",
                column: "DietPlanMealId");

            migrationBuilder.AddForeignKey(
                name: "FK_MealLogs_DietPlanMeals_DietPlanMealId",
                table: "MealLogs",
                column: "DietPlanMealId",
                principalTable: "DietPlanMeals",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealLogs_DietPlanMeals_DietPlanMealId",
                table: "MealLogs");

            migrationBuilder.DropIndex(
                name: "IX_MealLogs_DietPlanMealId",
                table: "MealLogs");

            migrationBuilder.DropColumn(
                name: "DietPlanMealId",
                table: "MealLogs");
        }
    }
}
