using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainerAssignmentHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TrainerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerAssignmentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainerAssignmentHistories_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainerAssignmentHistories_Users_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainerAssignmentHistories_ClientId",
                table: "TrainerAssignmentHistories",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerAssignmentHistories_TrainerId",
                table: "TrainerAssignmentHistories",
                column: "TrainerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainerAssignmentHistories");
        }
    }
}
