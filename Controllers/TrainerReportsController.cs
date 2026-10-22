using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using HealthTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    public class TrainerReportsController : TrainerBaseController
    {
        private readonly IExportService _exportService;

        public TrainerReportsController(
            HealthTrackDbContext context,
            UserManager<Person> userManager,
            IExportService exportService)
            : base(context, userManager)
        {
            _exportService = exportService;
        }

        public async Task<IActionResult> Index()
        {
            var trainerId = CurrentUserId;
            var trainer = await Context.Trainers.FirstAsync(t => t.Id == trainerId);

            var assignedClientIds = await Context.ClientTrainerAssignments
                .Where(a => a.TrainerId == trainerId)
                .Select(a => a.ClientId)
                .ToListAsync();

            var performance = await GetAssignedClientPerformanceAsync(assignedClientIds);
            var activities = await GetPopularActivitiesAsync(assignedClientIds, 8);

            var pastWeek = DateTime.Today.AddDays(-6);
            var activePastWeek = performance.Count(p => p.LastLoginAt >= pastWeek);
            var totalWorkouts = performance.Sum(p => p.WorkoutCount);
            var totalMeals = performance.Sum(p => p.MealLogCount);
            var avgWorkouts = performance.Count == 0 ? 0 : Math.Round(performance.Average(p => p.WorkoutCount), 1);

            var model = new TrainerReportsViewModel
            {
                TrainerName = trainer.Name,
                TotalClients = performance.Count,
                ActiveClientsPastWeek = activePastWeek,
                TotalWorkoutsLogged = totalWorkouts,
                TotalMealsLogged = totalMeals,
                AverageWorkoutsPerClient = avgWorkouts,
                ClientPerformances = performance,
                PopularActivities = activities
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportReport(string format = "csv")
        {
            var trainerId = CurrentUserId;
            var trainer = await Context.Trainers.FirstAsync(t => t.Id == trainerId);

            var assignedClientIds = await Context.ClientTrainerAssignments
                .Where(a => a.TrainerId == trainerId)
                .Select(a => a.ClientId)
                .ToListAsync();

            var performance = await GetAssignedClientPerformanceAsync(assignedClientIds);
            var activities = await GetPopularActivitiesAsync(assignedClientIds, 10);

            var sections = new List<ReportSection>
            {
                new ReportSection("Assigned Client Performance",
                    new[] { "Client Name", "Email", "Start Weight (kg)", "Current Weight (kg)", "Change (kg)", "Current BMI", "Workouts", "Total Minutes", "Calories Burned", "Meals Logged", "Last Login" },
                    performance.Select(p => new object?[]
                    {
                        p.Name, p.Email, p.StartWeightKg, p.CurrentWeightKg, p.WeightChangeKg, p.CurrentBmi,
                        p.WorkoutCount, p.TotalMinutes, p.TotalCaloriesBurned, p.MealLogCount, p.LastLoginAt
                    })),
                new ReportSection("Popular Routines (Your Clients)",
                    new[] { "Activity", "Sessions", "Clients", "Total Minutes" },
                    activities.Select(a => new object?[] { a.Activity, a.Sessions, a.ClientCount, a.TotalMinutes })),
                new ReportSection("Client Engagement Summary",
                    new[] { "Metric", "Value" },
                    new[]
                    {
                        new object?[] { "Total Assigned Clients", performance.Count },
                        new object?[] { "Active in Last 7 Days", performance.Count(p => p.LastLoginAt >= DateTime.Today.AddDays(-6)) },
                        new object?[] { "Total Workouts Completed", performance.Sum(p => p.WorkoutCount) },
                        new object?[] { "Average Workouts per Client", performance.Count == 0 ? 0 : Math.Round(performance.Average(p => p.WorkoutCount), 1) }
                    })
            };

            await AuditAsync("Exported", "TrainerClientReport", null, format);
            var fileName = $"Trainer_Report_{trainer.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}";

            if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                return File(_exportService.ToPdf($"HealthTrack - Coach {trainer.Name} Client Report", sections), "application/pdf", fileName + ".pdf");
            }

            return File(_exportService.ToCsv(sections), "text/csv", fileName + ".csv");
        }

        private async Task<List<ClientPerformanceViewModel>> GetAssignedClientPerformanceAsync(List<string> clientIds)
        {
            return await Context.Clients
                .Where(c => clientIds.Contains(c.Id))
                .Select(c => new ClientPerformanceViewModel
                {
                    ClientId = c.Id,
                    Name = c.Name,
                    Email = c.Email ?? string.Empty,
                    HeightCm = c.HeightCm,
                    StartWeightKg = c.HealthMetrics.OrderBy(m => m.RecordedAt).Select(m => (double?)m.WeightKg).FirstOrDefault(),
                    CurrentWeightKg = c.HealthMetrics.OrderByDescending(m => m.RecordedAt).Select(m => (double?)m.WeightKg).FirstOrDefault(),
                    WorkoutCount = c.WorkoutLogs.Count(),
                    TotalMinutes = c.WorkoutLogs.Sum(w => w.DurationMinutes),
                    TotalCaloriesBurned = c.WorkoutLogs.Sum(w => w.CaloriesBurned),
                    MealLogCount = c.MealLogs.Count(),
                    LastLoginAt = c.LastLoginAt
                })
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        private async Task<List<ActivityStatViewModel>> GetPopularActivitiesAsync(List<string> clientIds, int count)
        {
            return await Context.WorkoutLogs
                .Where(w => clientIds.Contains(w.ClientId))
                .GroupBy(w => w.Activity)
                .Select(g => new ActivityStatViewModel
                {
                    Activity = g.Key,
                    Sessions = g.Count(),
                    ClientCount = g.Select(w => w.ClientId).Distinct().Count(),
                    TotalMinutes = g.Sum(w => w.DurationMinutes)
                })
                .OrderByDescending(a => a.Sessions)
                .Take(count)
                .ToListAsync();
        }
    }
}
