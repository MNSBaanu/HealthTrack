using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using HealthTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    // Trainer dashboard, client progress tracking and feedback.
    public class TrainerController : TrainerBaseController
    {
        private readonly ITrainerFeedbackService _feedbackService;
        private readonly ITrainerScheduleService _scheduleService;
        private readonly AppointmentTime _time;
        private readonly INotificationService _notifications;

        public TrainerController(
            HealthTrackDbContext context,
            UserManager<Person> userManager,
            ITrainerFeedbackService feedbackService,
            ITrainerScheduleService scheduleService,
            AppointmentTime time,
            INotificationService notifications)
            : base(context, userManager)
        {
            _notifications = notifications;
            _feedbackService = feedbackService;
            _scheduleService = scheduleService;
            _time = time;
        }

        // Dashboard with assigned clients, pending appointments, recent activity and feedback.
        public async Task<IActionResult> Index()
        {
            var trainerId = CurrentUserId;
            var trainer = await Context.Trainers.FirstAsync(t => t.Id == trainerId);

            var assignedClientIds = await Context.ClientTrainerAssignments
                .Where(a => a.TrainerId == trainerId)
                .Select(a => a.ClientId)
                .ToListAsync();

            var clientSummaries = await LoadClientSummariesAsync(trainerId, assignedClientIds, includeActivity: true);

            var recentActivities = new List<ClientRecentActivityViewModel>();

            var recentWorkouts = await Context.WorkoutLogs
                .Where(w => assignedClientIds.Contains(w.ClientId))
                .Include(w => w.Client)
                .OrderByDescending(w => w.LoggedAt)
                .Take(5)
                .Select(w => new ClientRecentActivityViewModel
                {
                    ClientId = w.ClientId,
                    ClientName = w.Client != null ? w.Client.Name : "Client",
                    ActivityType = "Workout",
                    Summary = $"{w.Activity} ({w.DurationMinutes} min, {w.CaloriesBurned} kcal)",
                    Timestamp = w.LoggedAt
                })
                .ToListAsync();

            var recentMeals = await Context.MealLogs
                .Where(m => assignedClientIds.Contains(m.ClientId))
                .Include(m => m.Client)
                .OrderByDescending(m => m.LoggedAt)
                .Take(5)
                .Select(m => new ClientRecentActivityViewModel
                {
                    ClientId = m.ClientId,
                    ClientName = m.Client != null ? m.Client.Name : "Client",
                    ActivityType = "Meal",
                    Summary = $"{m.MealType}: {m.FoodItems} ({m.Calories} kcal)",
                    Timestamp = m.LoggedAt
                })
                .ToListAsync();

            recentActivities.AddRange(recentWorkouts);
            recentActivities.AddRange(recentMeals);
            recentActivities = recentActivities.OrderByDescending(a => a.Timestamp).Take(8).ToList();

            var recentFeedbacks = await Context.TrainerFeedbacks
                .Where(f => f.TrainerId == trainerId)
                .Include(f => f.Client)
                .OrderByDescending(f => f.CreatedAtUtc)
                .Take(5)
                .Select(f => new TrainerFeedbackItemViewModel
                {
                    Id = f.Id,
                    ClientId = f.ClientId,
                    ClientName = f.Client != null ? f.Client.Name : "Client",
                    Title = f.Title,
                    Comments = f.Comments,
                    Category = f.Category,
                    CreatedAtUtc = f.CreatedAtUtc,
                    IsReadByClient = f.IsReadByClient
                })
                .ToListAsync();

            var schedule = await _scheduleService.GetScheduleAsync(trainerId);

            var model = new TrainerDashboardViewModel
            {
                TrainerName = trainer.Name,
                TotalAssignedClients = clientSummaries.Count,
                PendingAppointmentsCount = schedule.PendingAppointments.Count,
                RescheduleRequestsCount = schedule.RescheduleRequests.Count,
                ActiveWorkoutPlansCount = await Context.WorkoutPlans.CountAsync(p => p.TrainerId == trainerId && p.IsActive),
                ActiveDietPlansCount = await Context.DietPlans.CountAsync(p => p.TrainerId == trainerId && p.IsActive),
                AssignedClients = clientSummaries,
                PendingAppointments = schedule.PendingAppointments.Take(4).ToList(),
                RecentActivities = recentActivities,
                RecentFeedbacks = recentFeedbacks
            };

            return View(model);
        }

        // Lists the trainer's assigned clients with progress summaries.
        public async Task<IActionResult> Clients()
        {
            var trainerId = CurrentUserId;
            var assignedClientIds = await Context.ClientTrainerAssignments
                .Where(a => a.TrainerId == trainerId)
                .Select(a => a.ClientId)
                .ToListAsync();

            var list = await LoadClientSummariesAsync(trainerId, assignedClientIds, includeActivity: false);

            return View(list);
        }

        // Builds one summary row per client in SQL instead of loading every log and metric.
        private async Task<List<TrainerClientSummaryViewModel>> LoadClientSummariesAsync(string trainerId, List<string> clientIds, bool includeActivity)
        {
            var rows = await Context.Clients
                .Where(c => clientIds.Contains(c.Id))
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Email,
                    c.FitnessGoal,
                    c.HeightCm,
                    c.LastLoginAt,
                    LatestWeight = c.HealthMetrics.OrderByDescending(m => m.RecordedAt).Select(m => (double?)m.WeightKg).FirstOrDefault(),
                    LastMetric = c.HealthMetrics.Max(m => (DateTime?)m.RecordedAt),
                    LastWorkout = includeActivity ? c.WorkoutLogs.Max(w => (DateTime?)w.LoggedAt) : null,
                    LastMeal = includeActivity ? c.MealLogs.Max(m => (DateTime?)m.LoggedAt) : null,
                    ActiveWorkoutPlans = c.WorkoutPlans.Count(p => p.TrainerId == trainerId && p.IsActive),
                    ActiveDietPlans = c.DietPlans.Count(p => p.TrainerId == trainerId && p.IsActive)
                })
                .ToListAsync();

            return rows.Select(c =>
            {
                var bmi = c.LatestWeight.HasValue ? HealthMetric.CalculateBmi(c.LatestWeight.Value, c.HeightCm) : null;
                var lastActive = includeActivity
                    ? new[] { c.LastWorkout, c.LastMeal, c.LastMetric, c.LastLoginAt }.Max()
                    : c.LastLoginAt;

                return new TrainerClientSummaryViewModel
                {
                    ClientId = c.Id,
                    Name = c.Name,
                    Email = c.Email ?? string.Empty,
                    FitnessGoal = c.FitnessGoal,
                    HeightCm = c.HeightCm,
                    CurrentWeightKg = c.LatestWeight,
                    CurrentBmi = bmi,
                    BmiCategory = bmi.HasValue ? HealthMetric.GetBmiCategory(bmi.Value) : null,
                    LastActiveAt = lastActive,
                    ActiveWorkoutPlansCount = c.ActiveWorkoutPlans,
                    ActiveDietPlansCount = c.ActiveDietPlans
                };
            }).OrderBy(c => c.Name).ToList();
        }

        // Shows an assigned client's metrics, logs, plans and feedback.
        public async Task<IActionResult> ClientDetails(string id)
        {
            var trainerId = CurrentUserId;
            var isAssigned = await Context.ClientTrainerAssignments
                .AnyAsync(a => a.TrainerId == trainerId && a.ClientId == id);

            if (!isAssigned)
            {
                TempData["Error"] = "This client is not currently assigned to you.";
                return RedirectToAction(nameof(Clients));
            }

            await AuditAsync("Viewed", "ClientDetails", id);
            var client = await Context.Clients
                .Include(c => c.HealthMetrics.OrderByDescending(m => m.RecordedAt))
                .Include(c => c.WorkoutLogs.OrderByDescending(w => w.LoggedAt))
                .Include(c => c.MealLogs.OrderByDescending(m => m.LoggedAt))
                .Include(c => c.WorkoutPlans.Where(p => p.TrainerId == trainerId)).ThenInclude(p => p.Items)
                .Include(c => c.DietPlans.Where(p => p.TrainerId == trainerId)).ThenInclude(p => p.Meals)
                .Include(c => c.Feedbacks.Where(f => f.TrainerId == trainerId).OrderByDescending(f => f.CreatedAtUtc))
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null) return NotFound();

            var metrics = client.HealthMetrics.OrderBy(m => m.RecordedAt).ToList();
            var startWeight = metrics.FirstOrDefault()?.WeightKg;
            var currentWeight = metrics.LastOrDefault()?.WeightKg;
            var weightChange = startWeight.HasValue && currentWeight.HasValue
                ? Math.Round(currentWeight.Value - startWeight.Value, 1)
                : (double?)null;

            var currentBmi = currentWeight.HasValue
                ? HealthMetric.CalculateBmi(currentWeight.Value, client.HeightCm)
                : (double?)null;

            var model = new TrainerClientDetailsViewModel
            {
                Client = client,
                StartWeightKg = startWeight,
                CurrentWeightKg = currentWeight,
                WeightChangeKg = weightChange,
                CurrentBmi = currentBmi,
                BmiCategory = currentBmi.HasValue ? HealthMetric.GetBmiCategory(currentBmi.Value) : null,
                MetricsHistory = client.HealthMetrics.OrderByDescending(m => m.RecordedAt).Take(15).ToList(),
                WorkoutHistory = client.WorkoutLogs.OrderByDescending(w => w.LoggedAt).Take(15).ToList(),
                MealHistory = client.MealLogs.OrderByDescending(m => m.LoggedAt).Take(15).ToList(),
                WorkoutPlans = client.WorkoutPlans.OrderByDescending(p => p.CreatedAtUtc).ToList(),
                DietPlans = client.DietPlans.OrderByDescending(p => p.CreatedAtUtc).ToList(),
                Feedbacks = client.Feedbacks.OrderByDescending(f => f.CreatedAtUtc).ToList(),
                CheckIns = await Context.WeeklyCheckIns.AsNoTracking()
                    .Where(c => c.ClientId == client.Id && c.TrainerId == trainerId)
                    .OrderByDescending(c => c.RequestedAtUtc).Take(12).ToListAsync(),
                NewFeedbackForm = new TrainerFeedbackFormViewModel
                {
                    ClientId = client.Id,
                    ClientName = client.Name
                }
            };

            return View(model);
        }

        // Asks a client to complete a weekly check-in.
        [HttpPost]
        public async Task<IActionResult> RequestCheckIn(string clientId, string? note)
        {
            var trainerId = CurrentUserId;
            var isAssigned = await Context.ClientTrainerAssignments
                .AnyAsync(a => a.TrainerId == trainerId && a.ClientId == clientId);
            if (!isAssigned)
            {
                TempData["Error"] = "Client is not assigned to you.";
                return RedirectToAction(nameof(Clients));
            }

            note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            if (note?.Length > 300)
            {
                TempData["Error"] = "The note can be at most 300 characters.";
                return RedirectToAction(nameof(ClientDetails), new { id = clientId });
            }

            if (await Context.WeeklyCheckIns.AnyAsync(c => c.ClientId == clientId && c.TrainerId == trainerId && c.RespondedAtUtc == null))
            {
                TempData["Error"] = "This client already has a check-in waiting for an answer.";
                return RedirectToAction(nameof(ClientDetails), new { id = clientId });
            }

            Context.WeeklyCheckIns.Add(new WeeklyCheckIn { TrainerId = trainerId, ClientId = clientId, Note = note });
            try
            {
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (exception.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
            {
                TempData["Error"] = "This client already has a check-in waiting for an answer.";
                return RedirectToAction(nameof(ClientDetails), new { id = clientId });
            }
            await _notifications.SendAsync(clientId, "Your Trainer asked for your weekly check-in. Open Feedback to answer.", NotificationType.General);
            TempData["Success"] = "Weekly check-in sent to the client.";
            return RedirectToAction(nameof(ClientDetails), new { id = clientId });
        }

        // Sends feedback to an assigned client.
        [HttpPost]
        public async Task<IActionResult> SendFeedback(
            [Bind(Prefix = nameof(TrainerClientDetailsViewModel.NewFeedbackForm))] TrainerFeedbackFormViewModel model)
        {
            var trainerId = CurrentUserId;
            var isAssigned = await Context.ClientTrainerAssignments
                .AnyAsync(a => a.TrainerId == trainerId && a.ClientId == model.ClientId);

            if (!isAssigned)
            {
                TempData["Error"] = "Client is not assigned to you.";
                return RedirectToAction(nameof(Clients));
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill in all required feedback fields.";
                return RedirectToAction(nameof(ClientDetails), new { id = model.ClientId });
            }

            await _feedbackService.AddFeedbackAsync(trainerId, model);
            TempData["Success"] = "Feedback sent successfully to the client.";
            return RedirectToAction(nameof(ClientDetails), new { id = model.ClientId });
        }

        // Shows the edit form for feedback.
        [HttpGet]
        public async Task<IActionResult> EditFeedback(int id)
        {
            var trainerId = CurrentUserId;
            var model = await _feedbackService.GetFeedbackForEditAsync(trainerId, id);

            if (model == null)
            {
                TempData["Error"] = "Feedback not found, or the client has already replied to it.";
                return RedirectToAction(nameof(Clients));
            }

            return View(model);
        }

        // Saves changes to feedback.
        [HttpPost]
        public async Task<IActionResult> EditFeedback(int id, TrainerFeedbackFormViewModel model)
        {
            var trainerId = CurrentUserId;
            if (!ModelState.IsValid)
            {
                model.ClientName = await Context.TrainerFeedbacks
                    .Where(f => f.Id == id && f.TrainerId == trainerId)
                    .Select(f => f.Client.Name).FirstOrDefaultAsync();
                return View(model);
            }

            var success = await _feedbackService.UpdateFeedbackAsync(trainerId, id, model);

            if (!success)
            {
                TempData["Error"] = "Feedback not found, or the client has already replied to it.";
                return RedirectToAction(nameof(Clients));
            }

            TempData["Success"] = "Feedback updated successfully.";

            // Get the client ID to redirect back to client details
            var feedback = await Context.TrainerFeedbacks.FirstOrDefaultAsync(f => f.Id == id);
            return RedirectToAction(nameof(ClientDetails), new { id = feedback?.ClientId });
        }

        // Deletes feedback.
        [HttpPost]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            var trainerId = CurrentUserId;
            var feedback = await Context.TrainerFeedbacks
                .FirstOrDefaultAsync(f => f.Id == id && f.TrainerId == trainerId);

            if (feedback == null)
            {
                TempData["Error"] = "Feedback not found or you don't have permission to delete it.";
                return RedirectToAction(nameof(Clients));
            }

            var clientId = feedback.ClientId;
            var success = await _feedbackService.DeleteFeedbackAsync(trainerId, id);

            if (!success)
            {
                TempData["Error"] = "Failed to delete feedback.";
                return RedirectToAction(nameof(ClientDetails), new { id = clientId });
            }

            TempData["Success"] = "Feedback deleted successfully.";
            return RedirectToAction(nameof(ClientDetails), new { id = clientId });
        }
    }
}
