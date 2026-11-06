using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services;

// Creates and manages workout and diet plans, limited to the trainer's assigned clients.
public class TrainerPlanService : ITrainerPlanService
{
    private readonly HealthTrackDbContext _context;
    private readonly INotificationService _notificationService;

    public TrainerPlanService(HealthTrackDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    // Checks that the client is assigned to the trainer.
    public Task<bool> IsClientAssignedToTrainerAsync(string trainerId, string clientId)
    {
        return _context.ClientTrainerAssignments.AnyAsync(a => a.TrainerId == trainerId && a.ClientId == clientId);
    }

    // Builds the client drop-down for plan forms.
    public async Task<List<SelectListItem>> GetAssignedClientSelectListAsync(string trainerId)
    {
        var clients = await _context.ClientTrainerAssignments
            .Where(a => a.TrainerId == trainerId)
            .OrderBy(a => a.Client.Name)
            .Select(a => new { a.ClientId, a.Client.Name, a.Client.FitnessGoal })
            .ToListAsync();
        return clients.Select(c => new SelectListItem
        {
            Value = c.ClientId,
            Text = c.Name + " (" + (c.FitnessGoal ?? "No goal set") + ")"
        }).ToList();
    }

    // Loads a workout plan owned by the trainer.
    public async Task<WorkoutPlan?> GetWorkoutPlanByIdAsync(int planId, string trainerId)
    {
        return await _context.WorkoutPlans
            .Include(p => p.Client)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == planId && p.TrainerId == trainerId && _context.ClientTrainerAssignments.Any(a => a.TrainerId == trainerId && a.ClientId == p.ClientId));
    }

    // Loads a diet plan owned by the trainer.
    public async Task<DietPlan?> GetDietPlanByIdAsync(int planId, string trainerId)
    {
        return await _context.DietPlans
            .Include(p => p.Client)
            .Include(p => p.Meals)
            .FirstOrDefaultAsync(p => p.Id == planId && p.TrainerId == trainerId && _context.ClientTrainerAssignments.Any(a => a.TrainerId == trainerId && a.ClientId == p.ClientId));
    }

    // Creates or updates a workout plan and its exercises, then notifies the client.
    public async Task<int> SaveWorkoutPlanAsync(string trainerId, WorkoutPlanFormViewModel model)
    {
        WorkoutPlan? plan;
        bool isNew = model.Id == 0;
        await using var operation = await AtomicOperation.BeginAsync(_context);

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
                .FirstOrDefaultAsync(p => p.Id == model.Id && p.TrainerId == trainerId && _context.ClientTrainerAssignments.Any(a => a.TrainerId == trainerId && a.ClientId == p.ClientId));
            if (plan == null) return 0;
        }

        plan.Title = model.Title.Trim();
        plan.Goal = model.Goal?.Trim();
        plan.Description = model.Description?.Trim();
        plan.StartDate = model.StartDate;
        plan.EndDate = model.EndDate;
        plan.IsActive = model.IsActive;

        // Existing exercises are updated in place so the client's completions and log links survive an edit.
        var posted = (model.Items ?? new()).Where(i => !string.IsNullOrWhiteSpace(i.ExerciseName)).ToList();
        var keptIds = posted.Where(i => i.Id != 0).Select(i => i.Id).ToHashSet();
        var removed = plan.Items.Where(i => !keptIds.Contains(i.Id)).ToList();
        if (removed.Count > 0)
        {
            var removedIds = removed.Select(i => i.Id).ToList();
            await _context.WorkoutLogs.Where(l => l.WorkoutPlanItemId != null && removedIds.Contains(l.WorkoutPlanItemId.Value))
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.WorkoutPlanItemId, (int?)null));
            _context.WorkoutPlanItems.RemoveRange(removed);
        }

        foreach (var input in posted)
        {
            var item = plan.Items.FirstOrDefault(i => i.Id != 0 && i.Id == input.Id);
            if (item == null)
            {
                item = new WorkoutPlanItem();
                plan.Items.Add(item);
            }
            item.DayOfWeek = input.DayOfWeek;
            item.ExerciseName = input.ExerciseName.Trim();
            item.Sets = input.Sets;
            item.Reps = input.Reps;
            item.DurationMinutes = input.DurationMinutes;
            item.RestSeconds = input.RestSeconds;
            item.Instructions = input.Instructions?.Trim();
        }

        await _context.SaveChangesAsync();
        await operation.CommitAsync();

        var message = isNew
            ? $"Your Trainer assigned a new workout plan: {plan.Title}"
            : $"Your workout plan '{plan.Title}' was updated by your Trainer.";

        await _notificationService.SendAsync(plan.ClientId, message, NotificationType.Workout);

        return plan.Id;
    }

    // Creates or updates a diet plan and its meals, then notifies the client.
    public async Task<int> SaveDietPlanAsync(string trainerId, DietPlanFormViewModel model)
    {
        DietPlan? plan;
        bool isNew = model.Id == 0;
        await using var operation = await AtomicOperation.BeginAsync(_context);

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
                .FirstOrDefaultAsync(p => p.Id == model.Id && p.TrainerId == trainerId && _context.ClientTrainerAssignments.Any(a => a.TrainerId == trainerId && a.ClientId == p.ClientId));
            if (plan == null) return 0;
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

        // Existing meals are updated in place so meals the client already logged stay linked.
        var posted = (model.Meals ?? new()).Where(m => !string.IsNullOrWhiteSpace(m.FoodRecommendations)).ToList();
        var keptIds = posted.Where(m => m.Id != 0).Select(m => m.Id).ToHashSet();
        var removed = plan.Meals.Where(m => !keptIds.Contains(m.Id)).ToList();
        if (removed.Count > 0)
        {
            var removedIds = removed.Select(m => m.Id).ToList();
            await _context.MealLogs.Where(l => l.DietPlanMealId != null && removedIds.Contains(l.DietPlanMealId.Value))
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.DietPlanMealId, (int?)null));
            _context.DietPlanMeals.RemoveRange(removed);
        }

        foreach (var input in posted)
        {
            var meal = plan.Meals.FirstOrDefault(m => m.Id != 0 && m.Id == input.Id);
            if (meal == null)
            {
                meal = new DietPlanMeal();
                plan.Meals.Add(meal);
            }
            meal.MealType = input.MealType;
            meal.FoodRecommendations = input.FoodRecommendations.Trim();
            meal.ApproximateCalories = input.ApproximateCalories;
            meal.Notes = input.Notes?.Trim();
        }

        await _context.SaveChangesAsync();
        await operation.CommitAsync();

        var message = isNew
            ? $"Your Trainer assigned a new diet plan: {plan.Title}"
            : $"Your diet plan '{plan.Title}' was updated by your Trainer.";

        await _notificationService.SendAsync(plan.ClientId, message, NotificationType.Meal);

        return plan.Id;
    }

    // Activates or pauses a workout plan.
    public async Task<bool> ToggleWorkoutPlanStatusAsync(int planId, string trainerId)
    {
        var plan = await _context.WorkoutPlans.FirstOrDefaultAsync(p => p.Id == planId && p.TrainerId == trainerId && _context.ClientTrainerAssignments.Any(a => a.TrainerId == trainerId && a.ClientId == p.ClientId));
        if (plan == null) return false;

        plan.IsActive = !plan.IsActive;
        await _context.SaveChangesAsync();
        return true;
    }

    // Activates or pauses a diet plan.
    public async Task<bool> ToggleDietPlanStatusAsync(int planId, string trainerId)
    {
        var plan = await _context.DietPlans.FirstOrDefaultAsync(p => p.Id == planId && p.TrainerId == trainerId && _context.ClientTrainerAssignments.Any(a => a.TrainerId == trainerId && a.ClientId == p.ClientId));
        if (plan == null) return false;

        plan.IsActive = !plan.IsActive;
        await _context.SaveChangesAsync();
        return true;
    }

    // Deletes a workout plan.
    public async Task<bool> DeleteWorkoutPlanAsync(int planId, string trainerId)
    {
        var plan = await _context.WorkoutPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == planId && p.TrainerId == trainerId && _context.ClientTrainerAssignments.Any(a => a.TrainerId == trainerId && a.ClientId == p.ClientId));

        if (plan == null) return false;

        var clientId = plan.ClientId;
        var planTitle = plan.Title;

        await using (var operation = await AtomicOperation.BeginAsync(_context))
        {
            var itemIds = plan.Items.Select(i => i.Id).ToList();
            await _context.WorkoutLogs.Where(l => l.WorkoutPlanItemId != null && itemIds.Contains(l.WorkoutPlanItemId.Value))
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.WorkoutPlanItemId, (int?)null));
            _context.WorkoutPlanItems.RemoveRange(plan.Items);
            _context.WorkoutPlans.Remove(plan);
            await _context.SaveChangesAsync();
            await operation.CommitAsync();
        }

        await _notificationService.SendAsync(clientId, $"Your workout plan '{planTitle}' has been deleted by your Trainer.", NotificationType.Workout);

        return true;
    }

    // Deletes a diet plan.
    public async Task<bool> DeleteDietPlanAsync(int planId, string trainerId)
    {
        var plan = await _context.DietPlans
            .Include(p => p.Meals)
            .FirstOrDefaultAsync(p => p.Id == planId && p.TrainerId == trainerId && _context.ClientTrainerAssignments.Any(a => a.TrainerId == trainerId && a.ClientId == p.ClientId));

        if (plan == null) return false;

        var clientId = plan.ClientId;
        var planTitle = plan.Title;

        await using (var operation = await AtomicOperation.BeginAsync(_context))
        {
            var mealIds = plan.Meals.Select(m => m.Id).ToList();
            await _context.MealLogs.Where(l => l.DietPlanMealId != null && mealIds.Contains(l.DietPlanMealId.Value))
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.DietPlanMealId, (int?)null));
            _context.DietPlanMeals.RemoveRange(plan.Meals);
            _context.DietPlans.Remove(plan);
            await _context.SaveChangesAsync();
            await operation.CommitAsync();
        }

        await _notificationService.SendAsync(clientId, $"Your diet plan '{planTitle}' has been deleted by your Trainer.", NotificationType.Meal);

        return true;
    }
}
