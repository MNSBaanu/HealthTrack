using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutPlanCompletions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkoutPlanCompletions",
                columns: table => new
                {
                    WorkoutPlanItemId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompletedOn = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutPlanCompletions", x => new { x.WorkoutPlanItemId, x.ClientId, x.CompletedOn });
                    table.ForeignKey(
                        name: "FK_WorkoutPlanCompletions_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkoutPlanCompletions_WorkoutPlanItems_WorkoutPlanItemId",
                        column: x => x.WorkoutPlanItemId,
                        principalTable: "WorkoutPlanItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlanCompletions_ClientId",
                table: "WorkoutPlanCompletions",
                column: "ClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkoutPlanCompletions");
        }
    }
}

