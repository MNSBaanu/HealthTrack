using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    public class MealLogsController : ClientBaseController
    {
        private readonly ILogger<MealLogsController> _logger;

        public MealLogsController(ILogger<MealLogsController> logger, HealthTrackDbContext context, UserManager<Person> userManager)
            : base(context, userManager)
        {
            _logger = logger;
        }

        // True when the plan meal already has a journal entry on that calendar day.
        private Task<bool> HasPlanLogOnAsync(int planMealId, DateTime day, int? exceptLogId = null)
        {
            var start = day.Date;
            var end = start.AddDays(1);
            return Context.MealLogs.AnyAsync(l => l.ClientId == CurrentUserId && l.DietPlanMealId == planMealId
                && l.LoggedAt >= start && l.LoggedAt < end && (exceptLogId == null || l.MealLogId != exceptLogId));
        }

        private static string? CleanFoodItems(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private Task<MealLog?> FindOwnedAsync(int id)
        {
            return Context.MealLogs.FirstOrDefaultAsync(m => m.MealLogId == id && m.ClientId == CurrentUserId);
        }

        public async Task<IActionResult> Index(DateTime? from = null, DateTime? to = null, string? q = null, MealType? mealType = null)
        {
            q = q?.Trim();
            if (!SetJournalFilters(from, to, q, mealType, meals: true)) return View(new List<MealLog>());
            var logs = await Context.MealLogs
                .Where(m => m.ClientId == CurrentUserId)
                .Where(m => (!from.HasValue || m.LoggedAt.Date >= from.Value.Date)
                    && (!to.HasValue || m.LoggedAt.Date <= to.Value.Date)
                    && (!mealType.HasValue || m.MealType == mealType.Value)
                    && (string.IsNullOrEmpty(q) || (m.FoodItems != null && m.FoodItems.Contains(q))))
                .OrderByDescending(m => m.LoggedAt)
                .ThenByDescending(m => m.MealLogId)
                .ToListAsync();
            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? planMealId, int? repeatId = null)
        {
            var log = new MealLog { LoggedAt = LocalNow };
            if (repeatId.HasValue)
            {
                if (planMealId.HasValue) return BadRequest();
                var previous = await FindOwnedAsync(repeatId.Value);
                if (previous == null) return NotFound();
                log.MealType = previous.MealType;
                log.FoodItems = previous.FoodItems;
                log.Calories = previous.Calories;
                ViewData["IsRepeat"] = true;
            }
            if (planMealId.HasValue)
            {
                var meal = await FindOwnedPlanMealAsync(planMealId.Value);
                if (meal == null)
                {
                    return NotFound();
                }
                if (!meal.CanLogOn(log.LoggedAt))
                {
                    TempData["Error"] = "This meal can only be logged on its scheduled day while the plan is active.";
                    return RedirectToAction("Diet", "ClientPlans", new { id = meal.DietPlanId });
                }
                if (await HasPlanLogOnAsync(meal.Id, log.LoggedAt))
                {
                    TempData["Error"] = "This meal is already logged for today.";
                    return RedirectToAction("Diet", "ClientPlans", new { id = meal.DietPlanId });
                }

                log.DietPlanMealId = meal.Id;
                log.MealType = meal.MealType;
                log.Calories = meal.ApproximateCalories ?? 0;
                log.FoodItems = meal.FoodRecommendations;
            }
            return View(log);
        }

        private Task<DietPlanMeal?> FindOwnedPlanMealAsync(int id)
        {
            return Context.DietPlanMeals.AsNoTracking().Include(m => m.DietPlan)
                .FirstOrDefaultAsync(m => m.Id == id && m.DietPlan!.ClientId == CurrentUserId);
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("MealType,Calories,FoodItems,LoggedAt,DietPlanMealId")] MealLog log)
        {
            log.ClientId = CurrentUserId;
            ModelState.Remove(nameof(MealLog.ClientId));
            ValidateNotInFuture(log.LoggedAt, nameof(log.LoggedAt));
            DietPlanMeal? planMeal = null;
            if (log.DietPlanMealId.HasValue)
            {
                planMeal = await FindOwnedPlanMealAsync(log.DietPlanMealId.Value);
                if (planMeal == null)
                {
                    return NotFound();
                }
                var today = LocalNow.Date;
                if (!planMeal.CanLogOn(today) || log.LoggedAt.Date != today)
                    ModelState.AddModelError(nameof(log.LoggedAt), "Planned meals must be logged for today, on their scheduled day while the plan is active.");
                else if (await HasPlanLogOnAsync(planMeal.Id, log.LoggedAt))
                    ModelState.AddModelError(string.Empty, "This meal is already logged for today.");
            }
            if (!ModelState.IsValid)
            {
                return View(log);
            }

            log.FoodItems = CleanFoodItems(log.FoodItems);

            Context.MealLogs.Add(log);
            await Context.SaveChangesAsync();
            _logger.LogInformation("Client {ClientId} logged a meal", log.ClientId);
            TempData["Success"] = "Meal logged.";
            return planMeal == null
                ? RedirectToAction(nameof(Index))
                : RedirectToAction("Diet", "ClientPlans", new { id = planMeal.DietPlanId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var log = await FindOwnedAsync(id);
            return log == null ? NotFound() : View(log);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("MealType,Calories,FoodItems,LoggedAt")] MealLog input)
        {
            var log = await FindOwnedAsync(id);
            if (log == null)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(MealLog.ClientId));
            ValidateNotInFuture(input.LoggedAt, nameof(input.LoggedAt));
            if (log.DietPlanMealId.HasValue)
            {
                var meal = await FindOwnedPlanMealAsync(log.DietPlanMealId.Value);
                if (meal == null) return NotFound();
                if ((meal.Day.HasValue && meal.Day.Value != input.LoggedAt.DayOfWeek)
                    || input.LoggedAt.Date < meal.DietPlan!.StartDate.Date
                    || (meal.DietPlan.EndDate.HasValue && input.LoggedAt.Date > meal.DietPlan.EndDate.Value.Date))
                    ModelState.AddModelError(nameof(input.LoggedAt), "The date must match the meal's scheduled day and fall within the plan's dates.");
                else if (await HasPlanLogOnAsync(meal.Id, input.LoggedAt, id))
                    ModelState.AddModelError(nameof(input.LoggedAt), "This meal is already logged on that day.");
            }
            if (!ModelState.IsValid)
            {
                input.MealLogId = id;
                return View(input);
            }

            log.MealType = input.MealType;
            log.Calories = input.Calories;
            log.FoodItems = CleanFoodItems(input.FoodItems);
            log.LoggedAt = input.LoggedAt;
            await Context.SaveChangesAsync();
            TempData["Success"] = "Meal updated.";
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

            Context.MealLogs.Remove(log);
            await Context.SaveChangesAsync();
            TempData["Success"] = "Meal deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
