using HealthTrack.Models;
using HealthTrack.Models.Data;
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

        private Task<MealLog?> FindOwnedAsync(int id)
        {
            return Context.MealLogs.FirstOrDefaultAsync(m => m.MealLogId == id && m.ClientId == CurrentUserId);
        }

        public async Task<IActionResult> Index()
        {
            var logs = await Context.MealLogs
                .Where(m => m.ClientId == CurrentUserId)
                .OrderByDescending(m => m.LoggedAt)
                .ToListAsync();
            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? planMealId)
        {
            var log = new MealLog { LoggedAt = DateTime.Now };
            if (planMealId.HasValue)
            {
                var meal = await FindOwnedPlanMealAsync(planMealId.Value);
                if (meal == null)
                {
                    return NotFound();
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
            return Context.DietPlanMeals.AsNoTracking()
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
            }
            if (!ModelState.IsValid)
            {
                return View(log);
            }

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
            if (!ModelState.IsValid)
            {
                input.MealLogId = id;
                return View(input);
            }

            log.MealType = input.MealType;
            log.Calories = input.Calories;
            log.FoodItems = input.FoodItems;
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
