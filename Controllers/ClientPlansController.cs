using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using HealthTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers;

// Read-only assigned plans. Personal journals remain in their existing controllers.
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
        var doneToday = await Context.WorkoutLogs.AsNoTracking()
            .Where(l => l.ClientId == CurrentUserId && l.WorkoutPlanItemId != null && itemIds.Contains(l.WorkoutPlanItemId.Value)
                && l.LoggedAt >= today && l.LoggedAt < today.AddDays(1))
            .Select(l => l.WorkoutPlanItemId!.Value).ToListAsync();
        return View(new ClientWorkoutPlanViewModel { Plan = plan, Today = today, DoneTodayItemIds = doneToday.ToHashSet() });
    }

    [HttpGet]
    public async Task<IActionResult> Diet(int id)
    {
        var plan = await Context.DietPlans.AsNoTracking().Include(p => p.Trainer).Include(p => p.Meals)
            .FirstOrDefaultAsync(p => p.Id == id && p.ClientId == CurrentUserId);
        if (plan == null) return NotFound();
        return View(new ClientDietPlanViewModel { Plan = plan, Today = Today });
    }
}
