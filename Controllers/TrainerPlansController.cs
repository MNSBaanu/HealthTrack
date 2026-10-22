using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using HealthTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    public class TrainerPlansController : TrainerBaseController
    {
        private readonly ITrainerPlanService _planService;
        private readonly AppointmentTime _time;
        private readonly TimeProvider _clock;

        public TrainerPlansController(
            HealthTrackDbContext context,
            UserManager<Person> userManager,
            ITrainerPlanService planService, AppointmentTime time, TimeProvider clock)
            : base(context, userManager)
        {
            _planService = planService;
            _time = time;
            _clock = clock;
        }

        private void ValidateNewPlanDates(DateTime startDate, DateTime? endDate)
        {
            var today = _time.ToLocal(_clock.GetUtcNow().UtcDateTime).Date;
            if (startDate.Date < today)
                ModelState.AddModelError("StartDate", "Start date cannot be in the past.");
            if (endDate.HasValue && endDate.Value.Date < startDate.Date)
                ModelState.AddModelError("EndDate", "End date cannot be before the start date.");
        }

        // ================= WORKOUT PLANS =================

        public async Task<IActionResult> WorkoutPlans(string? clientId)
        {
            var trainerId = CurrentUserId;
            var query = Context.WorkoutPlans
                .Include(p => p.Client)
                .Include(p => p.Items)
                .Where(p => p.TrainerId == trainerId);

            if (!string.IsNullOrWhiteSpace(clientId))
            {
                query = query.Where(p => p.ClientId == clientId);
            }

            var model = new WorkoutPlanListViewModel
            {
                Plans = await query.OrderByDescending(p => p.CreatedAtUtc).ToListAsync(),
                SelectedClientId = clientId,
                AssignedClients = await _planService.GetAssignedClientSelectListAsync(trainerId)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateWorkoutPlan(string? clientId)
        {
            var trainerId = CurrentUserId;
            var model = new WorkoutPlanFormViewModel
            {
                ClientId = clientId ?? string.Empty,
                AssignedClients = await _planService.GetAssignedClientSelectListAsync(trainerId),
                StartDate = default,
                Items = new List<WorkoutPlanItemInputModel> { new() }
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateWorkoutPlan(WorkoutPlanFormViewModel model)
        {
            ValidateNewPlanDates(model.StartDate, model.EndDate);
            var trainerId = CurrentUserId;
            if (!await _planService.IsClientAssignedToTrainerAsync(trainerId, model.ClientId))
            {
                ModelState.AddModelError(nameof(model.ClientId), "Selected client is not assigned to you.");
            }

            if (!ModelState.IsValid)
            {
                model.AssignedClients = await _planService.GetAssignedClientSelectListAsync(trainerId);
                return View(model);
            }

            await _planService.SaveWorkoutPlanAsync(trainerId, model);
            TempData["Success"] = "Workout plan created and assigned to the client.";
            return RedirectToAction(nameof(WorkoutPlans), new { clientId = model.ClientId });
        }

        [HttpGet]
        public async Task<IActionResult> EditWorkoutPlan(int id)
        {
            var trainerId = CurrentUserId;
            var plan = await _planService.GetWorkoutPlanByIdAsync(id, trainerId);
            if (plan == null) return NotFound();

            var model = new WorkoutPlanFormViewModel
            {
                Id = plan.Id,
                ClientId = plan.ClientId,
                ClientName = plan.Client?.Name,
                Title = plan.Title,
                Goal = plan.Goal,
                Description = plan.Description,
                StartDate = plan.StartDate,
                EndDate = plan.EndDate,
                IsActive = plan.IsActive,
                AssignedClients = await _planService.GetAssignedClientSelectListAsync(trainerId),
                Items = plan.Items.OrderBy(i => i.DayOfWeek).Select(i => new WorkoutPlanItemInputModel
                {
                    Id = i.Id,
                    DayOfWeek = i.DayOfWeek,
                    ExerciseName = i.ExerciseName,
                    Sets = i.Sets,
                    Reps = i.Reps,
                    DurationMinutes = i.DurationMinutes,
                    RestSeconds = i.RestSeconds,
                    Instructions = i.Instructions
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditWorkoutPlan(WorkoutPlanFormViewModel model)
        {
            var trainerId = CurrentUserId;
            if (!ModelState.IsValid)
            {
                model.AssignedClients = await _planService.GetAssignedClientSelectListAsync(trainerId);
                return View(model);
            }

            await _planService.SaveWorkoutPlanAsync(trainerId, model);
            TempData["Success"] = "Workout plan updated successfully.";
            return RedirectToAction(nameof(WorkoutPlans), new { clientId = model.ClientId });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleWorkoutPlan(int id)
        {
            var success = await _planService.ToggleWorkoutPlanStatusAsync(id, CurrentUserId);
            if (!success) return NotFound();

            TempData["Success"] = "Workout plan status updated.";
            return RedirectToAction(nameof(WorkoutPlans));
        }

        // ================= DIET PLANS =================

        public async Task<IActionResult> DietPlans(string? clientId)
        {
            var trainerId = CurrentUserId;
            var query = Context.DietPlans
                .Include(p => p.Client)
                .Include(p => p.Meals)
                .Where(p => p.TrainerId == trainerId);

            if (!string.IsNullOrWhiteSpace(clientId))
            {
                query = query.Where(p => p.ClientId == clientId);
            }

            var model = new DietPlanListViewModel
            {
                Plans = await query.OrderByDescending(p => p.CreatedAtUtc).ToListAsync(),
                SelectedClientId = clientId,
                AssignedClients = await _planService.GetAssignedClientSelectListAsync(trainerId)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateDietPlan(string? clientId)
        {
            var trainerId = CurrentUserId;
            var model = new DietPlanFormViewModel
            {
                ClientId = clientId ?? string.Empty,
                AssignedClients = await _planService.GetAssignedClientSelectListAsync(trainerId),
                StartDate = default,
                DailyCalorieTarget = 0,
                Meals = new List<DietPlanMealInputModel> { new() { MealType = (MealType)(-1) } }
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDietPlan(DietPlanFormViewModel model)
        {
            ValidateNewPlanDates(model.StartDate, model.EndDate);
            var trainerId = CurrentUserId;
            if (!await _planService.IsClientAssignedToTrainerAsync(trainerId, model.ClientId))
            {
                ModelState.AddModelError(nameof(model.ClientId), "Selected client is not assigned to you.");
            }

            if (!ModelState.IsValid)
            {
                model.AssignedClients = await _planService.GetAssignedClientSelectListAsync(trainerId);
                return View(model);
            }

            await _planService.SaveDietPlanAsync(trainerId, model);
            TempData["Success"] = "Diet plan created and assigned to the client.";
            return RedirectToAction(nameof(DietPlans), new { clientId = model.ClientId });
        }

        [HttpGet]
        public async Task<IActionResult> EditDietPlan(int id)
        {
            var trainerId = CurrentUserId;
            var plan = await _planService.GetDietPlanByIdAsync(id, trainerId);
            if (plan == null) return NotFound();

            var model = new DietPlanFormViewModel
            {
                Id = plan.Id,
                ClientId = plan.ClientId,
                ClientName = plan.Client?.Name,
                Title = plan.Title,
                DailyCalorieTarget = plan.DailyCalorieTarget,
                TargetProteinGrams = plan.TargetProteinGrams,
                TargetCarbsGrams = plan.TargetCarbsGrams,
                TargetFatGrams = plan.TargetFatGrams,
                Description = plan.Description,
                StartDate = plan.StartDate,
                EndDate = plan.EndDate,
                IsActive = plan.IsActive,
                AssignedClients = await _planService.GetAssignedClientSelectListAsync(trainerId),
                Meals = plan.Meals.Select(m => new DietPlanMealInputModel
                {
                    Id = m.Id,
                    MealType = m.MealType,
                    FoodRecommendations = m.FoodRecommendations,
                    ApproximateCalories = m.ApproximateCalories,
                    Notes = m.Notes
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditDietPlan(DietPlanFormViewModel model)
        {
            var trainerId = CurrentUserId;
            if (!ModelState.IsValid)
            {
                model.AssignedClients = await _planService.GetAssignedClientSelectListAsync(trainerId);
                return View(model);
            }

            await _planService.SaveDietPlanAsync(trainerId, model);
            TempData["Success"] = "Diet plan updated successfully.";
            return RedirectToAction(nameof(DietPlans), new { clientId = model.ClientId });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleDietPlan(int id)
        {
            var success = await _planService.ToggleDietPlanStatusAsync(id, CurrentUserId);
            if (!success) return NotFound();

            TempData["Success"] = "Diet plan status updated.";
            return RedirectToAction(nameof(DietPlans));
        }
    }
}
