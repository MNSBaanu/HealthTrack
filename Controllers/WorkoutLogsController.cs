using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Services;
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

        // True when the plan exercise already has a journal entry on that calendar day.
        private Task<bool> HasPlanLogOnAsync(int planItemId, DateTime day, int? exceptLogId = null)
        {
            var start = day.Date;
            var end = start.AddDays(1);
            return Context.WorkoutLogs.AnyAsync(l => l.ClientId == CurrentUserId && l.WorkoutPlanItemId == planItemId
                && l.LoggedAt >= start && l.LoggedAt < end && (exceptLogId == null || l.WorkoutLogId != exceptLogId));
        }

        private Task<WorkoutLog?> FindOwnedAsync(int id)
        {
            return Context.WorkoutLogs.FirstOrDefaultAsync(w => w.WorkoutLogId == id && w.ClientId == CurrentUserId);
        }

        public async Task<IActionResult> Index(DateTime? from = null, DateTime? to = null, string? q = null)
        {
            q = q?.Trim();
            if (!SetJournalFilters(from, to, q)) return View(new List<WorkoutLog>());
            var logs = await Context.WorkoutLogs
                .Where(w => w.ClientId == CurrentUserId)
                .Where(w => (!from.HasValue || w.LoggedAt.Date >= from.Value.Date)
                    && (!to.HasValue || w.LoggedAt.Date <= to.Value.Date)
                    && (string.IsNullOrEmpty(q) || w.Activity.Contains(q)))
                .OrderByDescending(w => w.LoggedAt)
                .ThenByDescending(w => w.WorkoutLogId)
                .ToListAsync();
            return View(logs);
        }

        private Task<WorkoutPlanItem?> FindOwnedPlanItemAsync(int id)
        {
            return Context.WorkoutPlanItems.AsNoTracking().Include(i => i.WorkoutPlan)
                .FirstOrDefaultAsync(i => i.Id == id && i.WorkoutPlan!.ClientId == CurrentUserId);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? planItemId, int? repeatId = null)
        {
            var log = new WorkoutLog { LoggedAt = LocalNow };
            if (repeatId.HasValue)
            {
                if (planItemId.HasValue) return BadRequest();
                var previous = await FindOwnedAsync(repeatId.Value);
                if (previous == null) return NotFound();
                log.Activity = previous.Activity;
                log.DurationMinutes = previous.DurationMinutes;
                log.CaloriesBurned = previous.CaloriesBurned;
                ViewData["IsRepeat"] = true;
            }
            if (planItemId.HasValue)
            {
                var item = await FindOwnedPlanItemAsync(planItemId.Value);
                if (item == null)
                {
                    return NotFound();
                }

                if (!item.CanCompleteOn(log.LoggedAt))
                {
                    TempData["Error"] = "This exercise can only be marked as done on its scheduled day while the plan is active.";
                    return RedirectToAction("Workout", "ClientPlans", new { id = item.WorkoutPlanId });
                }
                if (await HasPlanLogOnAsync(item.Id, log.LoggedAt))
                {
                    TempData["Error"] = "This exercise already has a saved workout for today.";
                    return RedirectToAction("Workout", "ClientPlans", new { id = item.WorkoutPlanId });
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
                var today = LocalNow.Date;
                if (!planItem.CanCompleteOn(today) || log.LoggedAt.Date != today)
                {
                    ModelState.AddModelError(nameof(log.LoggedAt),
                        "Planned exercises can only be marked as done for today, on their scheduled day while the plan is active.");
                }
                else if (await HasPlanLogOnAsync(planItem.Id, log.LoggedAt))
                {
                    ModelState.AddModelError(string.Empty, "This exercise already has a saved workout for today.");
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
            if (log.WorkoutPlanItemId.HasValue)
            {
                var item = await FindOwnedPlanItemAsync(log.WorkoutPlanItemId.Value);
                if (item == null) return NotFound();
                if ((item.DayOfWeek.HasValue && item.DayOfWeek.Value != input.LoggedAt.DayOfWeek)
                    || input.LoggedAt.Date < item.WorkoutPlan!.StartDate.Date
                    || (item.WorkoutPlan.EndDate.HasValue && input.LoggedAt.Date > item.WorkoutPlan.EndDate.Value.Date))
                {
                    ModelState.AddModelError(nameof(input.LoggedAt),
                        "The workout date must match the exercise's scheduled day and fall within the plan's active dates.");
                }
                else if (await HasPlanLogOnAsync(item.Id, input.LoggedAt, id))
                {
                    ModelState.AddModelError(nameof(input.LoggedAt), "This exercise already has a saved workout on that day.");
                }
            }
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
