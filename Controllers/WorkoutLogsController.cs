using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    public class WorkoutLogsController : ClientBaseController
    {
        private readonly ILogger<WorkoutLogsController> _logger;

        public WorkoutLogsController(ILogger<WorkoutLogsController> logger, HealthTrackDbContext context, UserManager<Person> userManager)
            : base(context, userManager)
        {
            _logger = logger;
        }

        private Task<WorkoutLog?> FindOwnedAsync(int id)
        {
            return Context.WorkoutLogs.FirstOrDefaultAsync(w => w.WorkoutLogId == id && w.ClientId == CurrentUserId);
        }

        public async Task<IActionResult> Index()
        {
            var logs = await Context.WorkoutLogs
                .Where(w => w.ClientId == CurrentUserId)
                .OrderByDescending(w => w.LoggedAt)
                .ToListAsync();
            return View(logs);
        }

        private Task<WorkoutPlanItem?> FindOwnedPlanItemAsync(int id)
        {
            return Context.WorkoutPlanItems.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id && i.WorkoutPlan!.ClientId == CurrentUserId);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? planItemId)
        {
            var log = new WorkoutLog { LoggedAt = DateTime.Now };
            if (planItemId.HasValue)
            {
                var item = await FindOwnedPlanItemAsync(planItemId.Value);
                if (item == null)
                {
                    return NotFound();
                }

                log.WorkoutPlanItemId = item.Id;
                log.Activity = item.ExerciseName;
                log.DurationMinutes = item.DurationMinutes ?? 0;
            }
            return View(log);
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Activity,DurationMinutes,CaloriesBurned,LoggedAt,WorkoutPlanItemId")] WorkoutLog log)
        {
            log.ClientId = CurrentUserId;
            ModelState.Remove(nameof(WorkoutLog.ClientId));
            ValidateNotInFuture(log.LoggedAt, nameof(log.LoggedAt));
            WorkoutPlanItem? planItem = null;
            if (log.WorkoutPlanItemId.HasValue)
            {
                planItem = await FindOwnedPlanItemAsync(log.WorkoutPlanItemId.Value);
                if (planItem == null)
                {
                    return NotFound();
                }
            }
            if (!ModelState.IsValid)
            {
                return View(log);
            }

            log.Activity = log.Activity.Trim();
            Context.WorkoutLogs.Add(log);
            await Context.SaveChangesAsync();
            _logger.LogInformation("Client {ClientId} logged a workout", log.ClientId);
            TempData["Success"] = "Workout logged.";
            return planItem == null
                ? RedirectToAction(nameof(Index))
                : RedirectToAction("Workout", "ClientPlans", new { id = planItem.WorkoutPlanId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var log = await FindOwnedAsync(id);
            return log == null ? NotFound() : View(log);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Activity,DurationMinutes,CaloriesBurned,LoggedAt")] WorkoutLog input)
        {
            var log = await FindOwnedAsync(id);
            if (log == null)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(WorkoutLog.ClientId));
            ValidateNotInFuture(input.LoggedAt, nameof(input.LoggedAt));
            if (!ModelState.IsValid)
            {
                input.WorkoutLogId = id;
                return View(input);
            }

            log.Activity = input.Activity.Trim();
            log.DurationMinutes = input.DurationMinutes;
            log.CaloriesBurned = input.CaloriesBurned;
            log.LoggedAt = input.LoggedAt;
            await Context.SaveChangesAsync();
            TempData["Success"] = "Workout updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var log = await FindOwnedAsync(id);
            return log == null ? NotFound() : View(log);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var log = await FindOwnedAsync(id);
            if (log == null)
            {
                return NotFound();
            }

            Context.WorkoutLogs.Remove(log);
            await Context.SaveChangesAsync();
            TempData["Success"] = "Workout deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
