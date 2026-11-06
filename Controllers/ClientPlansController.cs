using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using HealthTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace HealthTrack.Controllers;

// Assigned plans and daily exercise checklists.
// Shows the workout and diet plans assigned to the client.
public class ClientPlansController : ClientBaseController
{
    private readonly AppointmentTime _time;
    private readonly TimeProvider _clock;
    public ClientPlansController(HealthTrackDbContext context, UserManager<Person> users,
        AppointmentTime time, TimeProvider clock) : base(context, users)
    {
        _time = time;
        _clock = clock;
    }
    private DateTime Today => _time.ToLocal(_clock.GetUtcNow().UtcDateTime).Date;

    // Lists all assigned workout and diet plans.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(new ClientPlansViewModel
        {
            Today = Today,
            WorkoutPlans = await Context.WorkoutPlans.AsNoTracking().Include(p => p.Trainer)
                .Where(p => p.ClientId == CurrentUserId)
                .OrderByDescending(p => p.IsActive).ThenByDescending(p => p.StartDate).ThenByDescending(p => p.Id).ToListAsync(),
            DietPlans = await Context.DietPlans.AsNoTracking().Include(p => p.Trainer)
                .Where(p => p.ClientId == CurrentUserId)
                .OrderByDescending(p => p.IsActive).ThenByDescending(p => p.StartDate).ThenByDescending(p => p.Id).ToListAsync()
        });
    }

    // Shows a workout plan with its exercises and completion status.
    [HttpGet]
    public async Task<IActionResult> Workout(int id, DateTime? date = null)
    {
        var plan = await Context.WorkoutPlans.AsNoTracking().Include(p => p.Trainer).Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && p.ClientId == CurrentUserId);
        if (plan == null) return NotFound();
        var itemIds = plan.Items.Select(i => i.Id).ToList();
        var today = Today;
        // Leave room for week navigation and avoid DateTime boundary overflow.
        if (date.HasValue && (date.Value.Year < 1900 || date.Value.Year > 9998)) return BadRequest();
        var model = new ClientWorkoutPlanViewModel
        {
            Plan = plan, Today = today, SelectedDate = date,
            Feedback = await PlanFeedbackAsync(plan.TrainerId, FeedbackCategory.Workout, plan.StartDate, plan.EndDate)
        };
        var weekEnd = model.WeekStart.AddDays(7);
        var logs = await Context.WorkoutLogs.AsNoTracking()
            .Where(l => l.ClientId == CurrentUserId && l.WorkoutPlanItemId != null && itemIds.Contains(l.WorkoutPlanItemId.Value)
                && l.LoggedAt >= model.WeekStart && l.LoggedAt < weekEnd)
            .Select(l => new { ItemId = l.WorkoutPlanItemId!.Value, Date = l.LoggedAt.Date }).ToListAsync();
        var checks = await Context.WorkoutPlanCompletions.AsNoTracking()
            .Where(c => c.ClientId == CurrentUserId && itemIds.Contains(c.WorkoutPlanItemId)
                && c.CompletedOn >= model.WeekStart && c.CompletedOn < weekEnd)
            .Select(c => new { ItemId = c.WorkoutPlanItemId, Date = c.CompletedOn }).ToListAsync();
        model.JournalPairs = logs.Select(l => (l.ItemId, l.Date)).ToHashSet();
        model.CompletedPairs = model.JournalPairs.Concat(checks.Select(c => (c.ItemId, c.Date.Date))).ToHashSet();
        return View(model);
    }

    // Marks a plan exercise as done or not done for today.
    [HttpPost]
    public async Task<IActionResult> SetWorkoutCompletion(int itemId, bool completed)
    {
        var item = await Context.WorkoutPlanItems.Include(i => i.WorkoutPlan)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.WorkoutPlan!.ClientId == CurrentUserId);
        if (item == null) return NotFound();
        var today = Today;
        var ajax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        IActionResult Reject(string message)
        {
            if (ajax) return BadRequest(new { message });
            TempData["Error"] = message;
            return RedirectToAction(nameof(Workout), new { id = item.WorkoutPlanId });
        }
        if (!item.CanCompleteOn(today))
            return Reject("You can only update this exercise on its scheduled day while the plan is active. Refresh the page to see the current schedule.");
        var logged = await Context.WorkoutLogs.AnyAsync(l => l.ClientId == CurrentUserId && l.WorkoutPlanItemId == itemId
            && l.LoggedAt >= today && l.LoggedAt < today.AddDays(1));
        if (logged && !completed)
            return Reject("This exercise has a saved workout. Edit or delete that entry in your workout journal to change its completion.");
        var check = await Context.WorkoutPlanCompletions
            .FirstOrDefaultAsync(c => c.WorkoutPlanItemId == itemId && c.ClientId == CurrentUserId && c.CompletedOn == today);
        if (completed && !logged && check == null)
            Context.WorkoutPlanCompletions.Add(new WorkoutPlanCompletion
                { WorkoutPlanItemId = itemId, ClientId = CurrentUserId, CompletedOn = today });
        else if (!completed && check != null)
            Context.WorkoutPlanCompletions.Remove(check);
        try { await Context.SaveChangesAsync(); }
        catch (DbUpdateException exception) when (completed && exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // Another request already saved the same daily completion.
        }
        catch (DbUpdateConcurrencyException) when (!completed)
        {
            // Another request already removed the check.
        }
        if (ajax) return Json(new { completed });
        return RedirectToAction(nameof(Workout), new { id = item.WorkoutPlanId });
    }

    // Shows a diet plan with its meals and calorie target.
    [HttpGet]
    public async Task<IActionResult> Diet(int id)
    {
        var plan = await Context.DietPlans.AsNoTracking().Include(p => p.Trainer).Include(p => p.Meals)
            .FirstOrDefaultAsync(p => p.Id == id && p.ClientId == CurrentUserId);
        if (plan == null) return NotFound();
        var mealIds = plan.Meals.Select(m => m.Id).ToList();
        var today = Today;
        var loggedToday = await Context.MealLogs.AsNoTracking()
            .Where(l => l.ClientId == CurrentUserId && l.DietPlanMealId != null && mealIds.Contains(l.DietPlanMealId.Value)
                && l.LoggedAt >= today && l.LoggedAt < today.AddDays(1))
            .Select(l => new { MealId = l.DietPlanMealId!.Value, l.Calories }).ToListAsync();
        var recent = await Context.MealLogs.AsNoTracking()
            .Where(l => l.ClientId == CurrentUserId && l.LoggedAt >= today.AddDays(-6) && l.LoggedAt < today.AddDays(1))
            .Select(l => new { l.LoggedAt, l.Calories }).ToListAsync();
        var caloriesByDate = recent.GroupBy(l => l.LoggedAt.Date).ToDictionary(g => g.Key, g => g.Sum(l => l.Calories));
        return View(new ClientDietPlanViewModel
        {
            Plan = plan, Today = today,
            LoggedTodayMealIds = loggedToday.Select(l => l.MealId).ToHashSet(),
            LoggedCaloriesToday = loggedToday.GroupBy(l => l.MealId).ToDictionary(g => g.Key, g => g.Sum(l => l.Calories)),
            CaloriesToday = caloriesByDate.GetValueOrDefault(today),
            CaloriesByDate = caloriesByDate,
            Feedback = await PlanFeedbackAsync(plan.TrainerId, FeedbackCategory.Nutrition, plan.StartDate, plan.EndDate)
        });
    }

    // Feedback from the plan's trainer on the same topic that was given while the plan was running.
    private async Task<List<TrainerFeedback>> PlanFeedbackAsync(string trainerId, FeedbackCategory category, DateTime start, DateTime? end)
    {
        var from = start.Date;
        var query = Context.TrainerFeedbacks.AsNoTracking()
            .Where(f => f.ClientId == CurrentUserId && f.TrainerId == trainerId && f.Category == category && f.CreatedAtUtc >= from);
        if (end.HasValue)
        {
            var until = end.Value.Date.AddDays(1);
            query = query.Where(f => f.CreatedAtUtc < until);
        }
        return await query.OrderByDescending(f => f.CreatedAtUtc).Take(5).ToListAsync();
    }
}
