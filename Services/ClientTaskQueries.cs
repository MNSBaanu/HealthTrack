using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services;

public static class ClientTaskQueries
{
    public static async Task<List<ClientTaskViewModel>> LoadAsync(HealthTrackDbContext context, string clientId, DateTime today)
    {
        var day = today.DayOfWeek;
        var tomorrow = today.AddDays(1);
        var workouts = await context.WorkoutPlanItems.AsNoTracking()
            .Where(i => i.WorkoutPlan!.ClientId == clientId && i.WorkoutPlan.IsActive
                && i.WorkoutPlan.StartDate.Date <= today && (!i.WorkoutPlan.EndDate.HasValue || i.WorkoutPlan.EndDate.Value.Date >= today)
                && (i.DayOfWeek == null || i.DayOfWeek == day))
            .OrderBy(i => i.WorkoutPlanId).ThenBy(i => i.Id).ToListAsync();
        var journalIds = await context.WorkoutLogs.Where(l => l.ClientId == clientId && l.WorkoutPlanItemId != null
                && l.LoggedAt >= today && l.LoggedAt < tomorrow)
            .Select(l => l.WorkoutPlanItemId!.Value).ToListAsync();
        var checkIds = await context.WorkoutPlanCompletions.Where(c => c.ClientId == clientId && c.CompletedOn == today)
            .Select(c => c.WorkoutPlanItemId).ToListAsync();
        var tasks = workouts.Select(i => new ClientTaskViewModel
        {
            Id = i.Id, PlanId = i.WorkoutPlanId, Title = i.ExerciseName, Kind = "Workout",
            Detail = string.Join(" · ", new[] { "Workout", i.Sets.HasValue ? $"{i.Sets} sets" : null,
                i.Reps.HasValue ? $"{i.Reps} reps" : null, i.DurationMinutes.HasValue ? $"{i.DurationMinutes} min" : null }.Where(s => s != null)),
            Completed = journalIds.Contains(i.Id) || checkIds.Contains(i.Id), JournalEntry = journalIds.Contains(i.Id)
        }).ToList();
        var meals = await context.DietPlanMeals.AsNoTracking()
            .Where(m => m.DietPlan!.ClientId == clientId && m.DietPlan.IsActive
                && m.DietPlan.StartDate.Date <= today && (!m.DietPlan.EndDate.HasValue || m.DietPlan.EndDate.Value.Date >= today)
                && (m.Day == null || m.Day == day))
            .OrderBy(m => m.Time).ThenBy(m => m.MealType).ThenBy(m => m.Id).ToListAsync();
        var mealIds = await context.MealLogs.Where(l => l.ClientId == clientId && l.DietPlanMealId != null
                && l.LoggedAt >= today && l.LoggedAt < tomorrow)
            .Select(l => l.DietPlanMealId!.Value).ToListAsync();
        tasks.AddRange(meals.Select(m => new ClientTaskViewModel
        {
            Id = m.Id, PlanId = m.DietPlanId, Kind = "Meal", Title = m.MealType.ToString(),
            Detail = $"{(m.Time.HasValue ? m.Time.Value.ToString("HH:mm") + " · " : "")}{m.FoodRecommendations}",
            Completed = mealIds.Contains(m.Id), JournalEntry = mealIds.Contains(m.Id)
        }));
        tasks.Add(new ClientTaskViewModel
        {
            Kind = "Measurement", Title = "Health measurements", Detail = "Optional check-in · record when needed",
            Completed = await context.HealthMetrics.AnyAsync(m => m.ClientId == clientId && m.RecordedAt >= today && m.RecordedAt < tomorrow)
        });
        return tasks;
    }
}
