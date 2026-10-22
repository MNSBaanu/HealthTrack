using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services;

public class TrainerPlanService : ITrainerPlanService
{
    private readonly HealthTrackDbContext _context;
    private readonly INotificationService _notificationService;

    public TrainerPlanService(HealthTrackDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public Task<bool> IsClientAssignedToTrainerAsync(string trainerId, string clientId)
    {
        return _context.ClientTrainerAssignments.AnyAsync(a => a.TrainerId == trainerId && a.ClientId == clientId);
    }

    public async Task<List<SelectListItem>> GetAssignedClientSelectListAsync(string trainerId)
    {
        return await _context.ClientTrainerAssignments
            .Where(a => a.TrainerId == trainerId)
            .OrderBy(a => a.Client.Name)
            .Select(a => new SelectListItem
            {
                Value = a.ClientId,
                Text = a.Client.Name + " (" + (a.Client.FitnessGoal ?? "No goal set") + ")"
            })
            .ToListAsync();
    }

    public async Task<WorkoutPlan?> GetWorkoutPlanByIdAsync(int planId, string trainerId)
    {
        return await _context.WorkoutPlans
            .Include(p => p.Client)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == planId && p.TrainerId == trainerId);
    }

    public async Task<DietPlan?> GetDietPlanByIdAsync(int planId, string trainerId)
    {
        return await _context.DietPlans
            .Include(p => p.Client)
            .Include(p => p.Meals)
            .FirstOrDefaultAsync(p => p.Id == planId && p.TrainerId == trainerId);
    }

    public async Task<int> SaveWorkoutPlanAsync(string trainerId, WorkoutPlanFormViewModel model)
    {
        WorkoutPlan plan;
        bool isNew = model.Id == 0;

        if (isNew)
        {
            plan = new WorkoutPlan
            {
                TrainerId = trainerId,
                ClientId = model.ClientId,
                CreatedAtUtc = DateTime.UtcNow
            };
            _context.WorkoutPlans.Add(plan);
        }
        else
        {
            plan = await _context.WorkoutPlans
                .Include(p => p.Items)
                .FirstAsync(p => p.Id == model.Id && p.TrainerId == trainerId);

            _context.WorkoutPlanItems.RemoveRange(plan.Items);
        }

        plan.Title = model.Title.Trim();
        plan.Goal = model.Goal?.Trim();
        plan.Description = model.Description?.Trim();
        plan.StartDate = model.StartDate;
        plan.EndDate = model.EndDate;
        plan.IsActive = model.IsActive;

        if (model.Items != null && model.Items.Count > 0)
        {
            foreach (var item in model.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ExerciseName)) continue;

                plan.Items.Add(new WorkoutPlanItem
                {
                    DayOfWeek = item.DayOfWeek,
                    ExerciseName = item.ExerciseName.Trim(),
                    Sets = item.Sets,
                    Reps = item.Reps,
                    DurationMinutes = item.DurationMinutes,
                    RestSeconds = item.RestSeconds,
                    Instructions = item.Instructions?.Trim()
                });
            }
        }

        await _context.SaveChangesAsync();

        var message = isNew
            ? $"Your Trainer assigned a new workout plan: {plan.Title}"
            : $"Your workout plan '{plan.Title}' was updated by your Trainer.";

        await _notificationService.SendAsync(plan.ClientId, message, NotificationType.Workout);

        return plan.Id;
    }

    public async Task<int> SaveDietPlanAsync(string trainerId, DietPlanFormViewModel model)
    {
        DietPlan plan;
        bool isNew = model.Id == 0;

        if (isNew)
        {
            plan = new DietPlan
            {
                TrainerId = trainerId,
                ClientId = model.ClientId,
                CreatedAtUtc = DateTime.UtcNow
            };
            _context.DietPlans.Add(plan);
        }
        else
        {
            plan = await _context.DietPlans
                .Include(p => p.Meals)
                .FirstAsync(p => p.Id == model.Id && p.TrainerId == trainerId);

            _context.DietPlanMeals.RemoveRange(plan.Meals);
        }

        plan.Title = model.Title.Trim();
        plan.DailyCalorieTarget = model.DailyCalorieTarget;
        plan.TargetProteinGrams = model.TargetProteinGrams;
        plan.TargetCarbsGrams = model.TargetCarbsGrams;
        plan.TargetFatGrams = model.TargetFatGrams;
        plan.Description = model.Description?.Trim();
        plan.StartDate = model.StartDate;
        plan.EndDate = model.EndDate;
        plan.IsActive = model.IsActive;

        if (model.Meals != null && model.Meals.Count > 0)
        {
            foreach (var meal in model.Meals)
            {
                if (string.IsNullOrWhiteSpace(meal.FoodRecommendations)) continue;

                plan.Meals.Add(new DietPlanMeal
                {
                    MealType = meal.MealType,
                    FoodRecommendations = meal.FoodRecommendations.Trim(),
                    ApproximateCalories = meal.ApproximateCalories,
                    Notes = meal.Notes?.Trim()
                });
            }
        }

        await _context.SaveChangesAsync();

        var message = isNew
            ? $"Your Trainer assigned a new diet plan: {plan.Title}"
            : $"Your diet plan '{plan.Title}' was updated by your Trainer.";

        await _notificationService.SendAsync(plan.ClientId, message, NotificationType.Meal);

        return plan.Id;
    }

    public async Task<bool> ToggleWorkoutPlanStatusAsync(int planId, string trainerId)
    {
        var plan = await _context.WorkoutPlans.FirstOrDefaultAsync(p => p.Id == planId && p.TrainerId == trainerId);
        if (plan == null) return false;

        plan.IsActive = !plan.IsActive;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleDietPlanStatusAsync(int planId, string trainerId)
    {
        var plan = await _context.DietPlans.FirstOrDefaultAsync(p => p.Id == planId && p.TrainerId == trainerId);
        if (plan == null) return false;

        plan.IsActive = !plan.IsActive;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteWorkoutPlanAsync(int planId, string trainerId)
    {
        var plan = await _context.WorkoutPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == planId && p.TrainerId == trainerId);

        if (plan == null) return false;

        var clientId = plan.ClientId;
        var planTitle = plan.Title;

        _context.WorkoutPlanItems.RemoveRange(plan.Items);
        _context.WorkoutPlans.Remove(plan);
        await _context.SaveChangesAsync();

        await _notificationService.SendAsync(clientId, $"Your workout plan '{planTitle}' has been deleted by your Trainer.", NotificationType.Workout);

        return true;
    }

    public async Task<bool> DeleteDietPlanAsync(int planId, string trainerId)
    {
        var plan = await _context.DietPlans
            .Include(p => p.Meals)
            .FirstOrDefaultAsync(p => p.Id == planId && p.TrainerId == trainerId);

        if (plan == null) return false;

        var clientId = plan.ClientId;
        var planTitle = plan.Title;

        _context.DietPlanMeals.RemoveRange(plan.Meals);
        _context.DietPlans.Remove(plan);
        await _context.SaveChangesAsync();

        await _notificationService.SendAsync(clientId, $"Your diet plan '{planTitle}' has been deleted by your Trainer.", NotificationType.Meal);

        return true;
    }
}
