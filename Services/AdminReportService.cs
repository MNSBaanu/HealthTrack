using System.Linq.Expressions;
using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services
{
    public class AdminReportService(HealthTrackDbContext context)
    {
        public const string PdfTitle = "HealthTrack - Client Performance Report";

        public async Task<List<ReportSection>> BuildSectionsAsync(DateTime? from, DateTime? to)
        {
            var performance = await GetClientPerformanceAsync(from, to);
            var activities = await GetPopularActivitiesAsync(10, from, to);
            var (planExercises, topPlans) = await GetPlanRoutinesAsync(10, from, to);

            return new List<ReportSection>
            {
                new ReportSection("Client Performance",
                    new[] { "Name", "Email", "Start Weight (kg)", "Current Weight (kg)", "Change (kg)", "BMI", "Workouts", "Minutes", "Calories Burned", "Meal Logs", "Weight Logs", "Appointments", "Active Days", "Last Login" },
                    performance.Select(p => new object?[]
                    {
                        p.Name, p.Email, p.StartWeightKg, p.CurrentWeightKg, p.WeightChangeKg, p.CurrentBmi,
                        p.WorkoutCount, p.TotalMinutes, p.TotalCaloriesBurned, p.MealLogCount, p.WeightLogCount, p.AppointmentCount, p.ActiveDays, p.LastLoginAt
                    })),
                new ReportSection("Popular Routines",
                    new[] { "Activity", "Sessions", "Clients", "Total Minutes" },
                    activities.Select(a => new object?[] { a.Activity, a.Sessions, a.ClientCount, a.TotalMinutes })),
                new ReportSection("Popular Plan Exercises",
                    new[] { "Exercise", "Plans", "Clients", "Completions" },
                    planExercises.Select(e => new object?[] { e.Exercise, e.PlanCount, e.ClientCount, e.Completions })),
                new ReportSection("Top Trainer Plans",
                    new[] { "Plan", "Trainer", "Client", "Exercises", "Completions" },
                    topPlans.Select(p => new object?[] { p.Title, p.Trainer, p.Client, p.ItemCount, p.Completions })),
                new ReportSection("Engagement",
                    new[] { "Metric", "Value" },
                    new[]
                    {
                        new object?[] { "Total clients", performance.Count },
                        new object?[] { "Clients active in last 7 days", performance.Count(p => p.LastLoginAt >= DateTime.Today.AddDays(-6)) },
                        new object?[] { "Clients with at least one workout", performance.Count(p => p.WorkoutCount > 0) },
                        new object?[] { "Average workouts per client", performance.Count == 0 ? 0 : Math.Round(performance.Average(p => p.WorkoutCount), 1) },
                        new object?[] { "Clients who logged a meal", performance.Count(p => p.MealLogCount > 0) },
                        new object?[] { "Clients who logged their weight", performance.Count(p => p.WeightLogCount > 0) },
                        new object?[] { "Appointments requested", performance.Sum(p => p.AppointmentCount) },
                        new object?[] { "Average active days per client", performance.Count == 0 ? 0 : Math.Round(performance.Average(p => p.ActiveDays), 1) }
                    })
            };
        }

        public Task<List<ActivityStatViewModel>> GetPopularActivitiesAsync(int count, DateTime? from = null, DateTime? to = null)
        {
            var start = from?.Date;
            var end = to?.Date.AddDays(1);
            return context.WorkoutLogs
                .Where(w => (start == null || w.LoggedAt >= start) && (end == null || w.LoggedAt < end))
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

        public async Task<(List<PlanExerciseStatViewModel> Exercises, List<PlanStatViewModel> Plans)> GetPlanRoutinesAsync(int count, DateTime? from, DateTime? to)
        {
            var start = from?.Date;
            var end = to?.Date.AddDays(1);
            var completions = await context.WorkoutLogs
                .Where(w => w.WorkoutPlanItemId != null
                    && (start == null || w.LoggedAt >= start) && (end == null || w.LoggedAt < end))
                .Select(w => new { ItemId = w.WorkoutPlanItemId!.Value, w.ClientId, Date = w.LoggedAt.Date })
                .Union(context.WorkoutPlanCompletions
                    .Where(c => (start == null || c.CompletedOn >= start) && (end == null || c.CompletedOn < end))
                    .Select(c => new { ItemId = c.WorkoutPlanItemId, c.ClientId, Date = c.CompletedOn }))
                .GroupBy(w => w.ItemId)
                .Select(g => new { ItemId = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.ItemId, x => x.Total);
            var items = await context.WorkoutPlanItems.AsNoTracking()
                .Select(i => new
                {
                    i.Id,
                    i.ExerciseName,
                    i.WorkoutPlanId,
                    i.WorkoutPlan!.Title,
                    i.WorkoutPlan.ClientId,
                    ClientName = i.WorkoutPlan.Client!.Name,
                    TrainerName = i.WorkoutPlan.Trainer!.Name
                })
                .ToListAsync();

            var exercises = items
                .GroupBy(i => i.ExerciseName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new PlanExerciseStatViewModel
                {
                    Exercise = g.Key,
                    PlanCount = g.Select(i => i.WorkoutPlanId).Distinct().Count(),
                    ClientCount = g.Select(i => i.ClientId).Distinct().Count(),
                    Completions = g.Sum(i => completions.GetValueOrDefault(i.Id))
                })
                .OrderByDescending(e => e.Completions).ThenByDescending(e => e.PlanCount).ThenBy(e => e.Exercise)
                .Take(count).ToList();
            var plans = items
                .GroupBy(i => i.WorkoutPlanId)
                .Select(g => new PlanStatViewModel
                {
                    Title = g.First().Title,
                    Trainer = g.First().TrainerName,
                    Client = g.First().ClientName,
                    ItemCount = g.Count(),
                    Completions = g.Sum(i => completions.GetValueOrDefault(i.Id))
                })
                .OrderByDescending(p => p.Completions).ThenBy(p => p.Title)
                .Take(count).ToList();
            return (exercises, plans);
        }

        public async Task<List<ClientPerformanceViewModel>> GetClientPerformanceAsync(DateTime? from = null, DateTime? to = null)
        {
            var start = from?.Date;
            var end = to?.Date.AddDays(1);
            var startUtc = start?.ToUniversalTime();
            var endUtc = end?.ToUniversalTime();
            Expression<Func<WorkoutLog, bool>> workoutInRange = w => (start == null || w.LoggedAt >= start) && (end == null || w.LoggedAt < end);
            Expression<Func<MealLog, bool>> mealInRange = m => (start == null || m.LoggedAt >= start) && (end == null || m.LoggedAt < end);
            Expression<Func<HealthMetric, bool>> metricInRange = m => (start == null || m.RecordedAt >= start) && (end == null || m.RecordedAt < end);

            var clients = await context.Clients
                .Select(c => new ClientPerformanceViewModel
                {
                    ClientId = c.Id,
                    Name = c.Name,
                    Email = c.Email ?? string.Empty,
                    HeightCm = c.HeightCm,
                    StartWeightKg = c.HealthMetrics.AsQueryable().Where(metricInRange).OrderBy(m => m.RecordedAt).Select(m => (double?)m.WeightKg).FirstOrDefault(),
                    CurrentWeightKg = c.HealthMetrics.AsQueryable().Where(metricInRange).OrderByDescending(m => m.RecordedAt).Select(m => (double?)m.WeightKg).FirstOrDefault(),
                    WeightLogCount = c.HealthMetrics.AsQueryable().Count(metricInRange),
                    WorkoutCount = c.WorkoutLogs.AsQueryable().Count(workoutInRange),
                    TotalMinutes = c.WorkoutLogs.AsQueryable().Where(workoutInRange).Sum(w => w.DurationMinutes),
                    TotalCaloriesBurned = c.WorkoutLogs.AsQueryable().Where(workoutInRange).Sum(w => w.CaloriesBurned),
                    MealLogCount = c.MealLogs.AsQueryable().Count(mealInRange),
                    LastLoginAt = c.LastLoginAt
                })
                .OrderBy(c => c.Name)
                .ToListAsync();

            var appointments = await context.Appointments
                .Where(a => (startUtc == null || a.RequestedAtUtc >= startUtc) && (endUtc == null || a.RequestedAtUtc < endUtc))
                .GroupBy(a => a.ClientId)
                .Select(g => new { ClientId = g.Key, Total = g.Count() })
                .ToDictionaryAsync(a => a.ClientId, a => a.Total);
            var activeDays = (await context.WorkoutLogs.Where(workoutInRange).Select(w => new { w.ClientId, w.LoggedAt.Date }).Distinct().ToListAsync())
                .Concat(await context.MealLogs.Where(mealInRange).Select(m => new { m.ClientId, m.LoggedAt.Date }).Distinct().ToListAsync())
                .Concat(await context.HealthMetrics.Where(metricInRange).Select(m => new { m.ClientId, m.RecordedAt.Date }).Distinct().ToListAsync())
                .Distinct()
                .GroupBy(d => d.ClientId)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var client in clients)
            {
                client.AppointmentCount = appointments.GetValueOrDefault(client.ClientId);
                client.ActiveDays = activeDays.GetValueOrDefault(client.ClientId);
            }
            return clients;
        }
    }
}
