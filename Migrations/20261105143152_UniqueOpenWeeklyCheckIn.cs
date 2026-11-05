using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthTrack.Migrations
{
    /// <inheritdoc />
    public partial class UniqueOpenWeeklyCheckIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep only the newest unanswered check-in per client and trainer before enforcing uniqueness.
            migrationBuilder.Sql(@"
WITH ranked AS (
    SELECT Id, ROW_NUMBER() OVER (PARTITION BY ClientId, TrainerId ORDER BY RequestedAtUtc DESC, Id DESC) AS rn
    FROM WeeklyCheckIns WHERE RespondedAtUtc IS NULL
)
DELETE FROM WeeklyCheckIns WHERE Id IN (SELECT Id FROM ranked WHERE rn > 1);");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyCheckIns_ClientId_TrainerId",
                table: "WeeklyCheckIns",
                columns: new[] { "ClientId", "TrainerId" },
                unique: true,
                filter: "[RespondedAtUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeeklyCheckIns_ClientId_TrainerId",
                table: "WeeklyCheckIns");
        }
    }
}
