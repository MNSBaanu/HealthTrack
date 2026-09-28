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

        [HttpGet]
        public IActionResult Create()
        {
            return View(new WorkoutLog { LoggedAt = DateTime.Now });
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Activity,DurationMinutes,CaloriesBurned,LoggedAt")] WorkoutLog log)
        {
            log.ClientId = CurrentUserId;
            ModelState.Remove(nameof(WorkoutLog.ClientId));
            ValidateNotInFuture(log.LoggedAt, nameof(log.LoggedAt));
            if (!ModelState.IsValid)
            {
                return View(log);
            }

            log.Activity = log.Activity.Trim();
            Context.WorkoutLogs.Add(log);
            await Context.SaveChangesAsync();
            _logger.LogInformation("Client {ClientId} logged a workout", log.ClientId);
            TempData["Success"] = "Workout logged.";
            return RedirectToAction(nameof(Index));
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
