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

    [HttpGet]
    public async Task<IActionResult> Workout(int id)
    {
        var plan = await Context.WorkoutPlans.AsNoTracking().Include(p => p.Trainer).Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && p.ClientId == CurrentUserId);
        if (plan == null) return NotFound();
        var itemIds = plan.Items.Select(i => i.Id).ToList();
        var today = Today;
        var model = new ClientWorkoutPlanViewModel { Plan = plan, Today = today };
        var weekEnd = model.WeekStart.AddDays(7);
        var logs = await Context.WorkoutLogs.AsNoTracking()
            .Where(l => l.ClientId == CurrentUserId && l.WorkoutPlanItemId != null && itemIds.Contains(l.WorkoutPlanItemId.Value)
                && l.LoggedAt >= model.WeekStart && l.LoggedAt < weekEnd)
            .Select(l => new { ItemId = l.WorkoutPlanItemId!.Value, Date = l.LoggedAt.Date }).ToListAsync();
        var checks = await Context.WorkoutPlanCompletions.AsNoTracking()
            .Where(c => c.ClientId == CurrentUserId && itemIds.Contains(c.WorkoutPlanItemId)
                && c.CompletedOn >= model.WeekStart && c.CompletedOn < weekEnd)
            .Select(c => new { ItemId = c.WorkoutPlanItemId, Date = c.CompletedOn }).ToListAsync();
        foreach (var item in plan.Items.Where(model.IsInPlanDates))
        {
            var date = model.ScheduledDate(item);
            if (logs.Any(l => l.ItemId == item.Id && l.Date == date)) model.JournalItemIds.Add(item.Id);
            if (model.JournalItemIds.Contains(item.Id) || checks.Any(c => c.ItemId == item.Id && c.Date == date))
                model.CompletedItemIds.Add(item.Id);
        }
        return View(model);
    }

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
            .Select(l => l.DietPlanMealId!.Value).ToListAsync();
        return View(new ClientDietPlanViewModel { Plan = plan, Today = today, LoggedTodayMealIds = loggedToday.ToHashSet() });
    }
}
