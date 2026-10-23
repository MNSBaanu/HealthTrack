using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using HealthTrack.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    public class ClientController : ClientBaseController
    {
        private readonly ILogger<ClientController> _logger;
        private readonly INotificationService _notificationService;
        private readonly IExportService _exportService;
        private readonly AppointmentTime _appointmentTime;
        private readonly TimeProvider _clock;

        public ClientController(ILogger<ClientController> logger, HealthTrackDbContext context, UserManager<Person> userManager,
            INotificationService notificationService, IExportService exportService,
            AppointmentTime appointmentTime, TimeProvider clock)
            : base(context, userManager)
        {
            _logger = logger;
            _notificationService = notificationService;
            _exportService = exportService;
            _appointmentTime = appointmentTime;
            _clock = clock;
        }

        public async Task<IActionResult> Index()
        {
            var clientId = CurrentUserId;
            await _notificationService.GenerateDailyRemindersAsync(clientId);

            var client = await Context.Clients.Include(c => c.TrainerAssignment).ThenInclude(a => a!.Trainer)
                .FirstAsync(c => c.Id == clientId);
            var metrics = await Context.HealthMetrics
                .Where(m => m.ClientId == clientId)
                .OrderBy(m => m.RecordedAt)
                .ToListAsync();

            var today = DateTime.Today;
            var nowUtc = _clock.GetUtcNow().UtcDateTime;
            var planToday = _appointmentTime.ToLocal(nowUtc).Date;
            var upcoming = await Context.Appointments.AsNoTracking()
                .Include(a => a.Slot).ThenInclude(s => s.Provider)
                .Where(a => a.ClientId == clientId && a.Slot.StartUtc > nowUtc
                    && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved))
                .OrderBy(a => a.Slot.StartUtc).Take(3).ToListAsync();
            var weekStart = today.AddDays(-6);
            var weekWorkouts = await Context.WorkoutLogs
                .Where(w => w.ClientId == clientId && w.LoggedAt >= weekStart)
                .ToListAsync();

            var todayDay = planToday.DayOfWeek;
            var plannedItemIds = await Context.WorkoutPlans.AsNoTracking()
                .Where(p => p.ClientId == clientId && p.IsActive && p.StartDate.Date <= planToday && (!p.EndDate.HasValue || p.EndDate.Value.Date >= planToday))
                .SelectMany(p => p.Items).Where(i => i.DayOfWeek == null || i.DayOfWeek == todayDay)
                .Select(i => i.Id).ToListAsync();
            var doneItemCount = await Context.WorkoutLogs.AsNoTracking()
                .Where(w => w.ClientId == clientId && w.WorkoutPlanItemId != null && plannedItemIds.Contains(w.WorkoutPlanItemId.Value)
                    && w.LoggedAt >= planToday && w.LoggedAt < planToday.AddDays(1))
                .Select(w => w.WorkoutPlanItemId).Distinct().CountAsync();
            var dietTarget = await Context.DietPlans.AsNoTracking()
                .Where(p => p.ClientId == clientId && p.IsActive && p.StartDate.Date <= planToday && (!p.EndDate.HasValue || p.EndDate.Value.Date >= planToday))
                .OrderByDescending(p => p.StartDate).Select(p => (int?)p.DailyCalorieTarget).FirstOrDefaultAsync();
            var workoutDates = await Context.WorkoutLogs.AsNoTracking().Where(w => w.ClientId == clientId).Select(w => w.LoggedAt).ToListAsync();
            var mealCount = await Context.MealLogs.CountAsync(m => m.ClientId == clientId);
            var goalReached = GoalProgressViewModel.Create(client, metrics)?.Reached == true;

            var model = new ClientDashboardViewModel
            {
                Client = client,
                PlanToday = planToday,
                DietTargetKcal = dietTarget,
                PlannedWorkoutsToday = plannedItemIds.Count,
                DoneWorkoutsToday = doneItemCount,
                Achievements = ClientAchievements.Compute(workoutDates, planToday, mealCount, metrics.Count, goalReached),
                AssignedWorkoutPlans = await Context.WorkoutPlans.AsNoTracking().Include(p => p.Trainer)
                    .Where(p => p.ClientId == clientId)
                    .OrderByDescending(p => p.IsActive && p.StartDate.Date <= planToday && (!p.EndDate.HasValue || p.EndDate.Value.Date >= planToday))
                    .ThenByDescending(p => p.IsActive).ThenByDescending(p => p.StartDate).ThenByDescending(p => p.Id).ToListAsync(),
                AssignedDietPlans = await Context.DietPlans.AsNoTracking().Include(p => p.Trainer)
                    .Where(p => p.ClientId == clientId)
                    .OrderByDescending(p => p.IsActive && p.StartDate.Date <= planToday && (!p.EndDate.HasValue || p.EndDate.Value.Date >= planToday))
                    .ThenByDescending(p => p.IsActive).ThenByDescending(p => p.StartDate).ThenByDescending(p => p.Id).ToListAsync(),
                UpcomingAppointments = upcoming.Select(_appointmentTime.ToViewModel).ToList(),
                AppointmentTimeZone = _appointmentTime.Label,
                LatestMetric = metrics.LastOrDefault(),
                WeightChangeKg = metrics.Count > 0 ? Math.Round(metrics[^1].WeightKg - metrics[0].WeightKg, 1) : null,
                StartingWeightKg = metrics.FirstOrDefault()?.WeightKg,
                MetricCount = metrics.Count,
                WorkoutsThisWeek = weekWorkouts.Count,
                MinutesThisWeek = weekWorkouts.Sum(w => w.DurationMinutes),
                CaloriesBurnedThisWeek = weekWorkouts.Sum(w => w.CaloriesBurned),
                CaloriesConsumedToday = await Context.MealLogs
                    .Where(m => m.ClientId == clientId && m.LoggedAt >= today)
                    .SumAsync(m => m.Calories),
                RecentWorkouts = await Context.WorkoutLogs
                    .Where(w => w.ClientId == clientId)
                    .OrderByDescending(w => w.LoggedAt)
                    .Take(5)
                    .ToListAsync(),
                RecentMeals = await Context.MealLogs
                    .Where(m => m.ClientId == clientId)
                    .OrderByDescending(m => m.LoggedAt)
                    .Take(5)
                    .ToListAsync(),
                Notifications = await _notificationService.GetVisibleAsync(clientId, unreadOnly: true)
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var client = await Context.Clients.FirstAsync(c => c.Id == CurrentUserId);
            var model = new ProfileViewModel
            {
                Email = client.Email ?? string.Empty,
                Name = client.Name,
                DateOfBirth = client.DateOfBirth,
                HeightCm = client.HeightCm,
                FitnessGoal = client.FitnessGoal,
                TargetWeightKg = client.TargetWeightKg,
                TargetDate = client.TargetDate,
                EmailNotificationsEnabled = client.EmailNotificationsEnabled,
                WorkoutReminderEnabled = client.WorkoutReminderEnabled,
                WorkoutReminderHour = client.WorkoutReminderHour,
                MealReminderEnabled = client.MealReminderEnabled,
                MealReminderHour = client.MealReminderHour,
                WeightReminderEnabled = client.WeightReminderEnabled,
                WeightReminderHour = client.WeightReminderHour,
                HasPhoto = client.ProfilePhoto != null
            };
            return View(model);
        }

        [HttpPost]
        [RequestSizeLimit(3 * 1024 * 1024)]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var client = await Context.Clients.FirstAsync(c => c.Id == CurrentUserId);
            model.Email = client.Email ?? string.Empty;
            model.HasPhoto = client.ProfilePhoto != null;
            ValidatePastDate(model.DateOfBirth, nameof(model.DateOfBirth));
            if (model.TargetDate.HasValue && model.TargetDate.Value.Date <= DateTime.Today)
                ModelState.AddModelError(nameof(model.TargetDate), "Choose a target date in the future.");
            byte[]? photo = null;
            string? photoType = null;
            if (model.Photo != null)
            {
                if (model.Photo.Length == 0 || model.Photo.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError(nameof(model.Photo), "Choose a JPG or PNG photo smaller than 2 MB.");
                else
                {
                    using var buffer = new MemoryStream();
                    await model.Photo.CopyToAsync(buffer);
                    photo = buffer.ToArray();
                    photoType = ProfilePhotoValidator.GetContentType(photo, model.Photo.FileName);
                    if (photoType == null)
                        ModelState.AddModelError(nameof(model.Photo), "Choose a valid JPG or PNG photo, up to 4096 pixels on each side.");
                }
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            client.Name = model.Name.Trim();
            client.DateOfBirth = model.DateOfBirth!.Value;
            client.HeightCm = model.HeightCm!.Value;
            client.FitnessGoal = model.FitnessGoal;
            client.TargetWeightKg = model.TargetWeightKg;
            client.TargetDate = model.TargetWeightKg.HasValue ? model.TargetDate : null;
            client.EmailNotificationsEnabled = model.EmailNotificationsEnabled;
            client.WorkoutReminderEnabled = model.WorkoutReminderEnabled;
            client.WorkoutReminderHour = model.WorkoutReminderHour;
            client.MealReminderEnabled = model.MealReminderEnabled;
            client.MealReminderHour = model.MealReminderHour;
            client.WeightReminderEnabled = model.WeightReminderEnabled;
            client.WeightReminderHour = model.WeightReminderHour;
            if (photo != null)
            {
                client.ProfilePhoto = photo;
                client.ProfilePhotoContentType = photoType;
            }
            await Context.SaveChangesAsync();

            TempData["Success"] = "Profile updated.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ProfilePhoto()
        {
            var photo = await Context.Clients.AsNoTracking()
                .Where(c => c.Id == CurrentUserId)
                .Select(c => new { c.ProfilePhoto, c.ProfilePhotoContentType }).FirstOrDefaultAsync();
            if (photo?.ProfilePhoto == null || photo.ProfilePhotoContentType == null)
                return NotFound();
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(photo.ProfilePhoto, photo.ProfilePhotoContentType);
        }

        public async Task<IActionResult> Progress()
        {
            var client = await Context.Clients.AsNoTracking().FirstAsync(c => c.Id == CurrentUserId);
            var metrics = await Context.HealthMetrics.AsNoTracking()
                .Where(m => m.ClientId == client.Id).OrderBy(m => m.RecordedAt).ToListAsync();
            ViewData["Goal"] = GoalProgressViewModel.Create(client, metrics);
            ViewData["HasGoal"] = client.TargetWeightKg.HasValue;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ProgressData(int days = 30)
        {
            var clientId = CurrentUserId;
            days = Math.Clamp(days, 7, 365);
            var from = DateTime.Today.AddDays(-(days - 1));
            var client = await Context.Clients.FirstAsync(c => c.Id == clientId);

            var weights = await Context.HealthMetrics
                .Where(m => m.ClientId == clientId && m.RecordedAt >= from)
                .OrderBy(m => m.RecordedAt)
                .ToListAsync();

            var model = new ProgressDataViewModel
            {
                Weight = weights.Select(m => new WeightPointViewModel
                {
                    Date = m.RecordedAt,
                    WeightKg = m.WeightKg,
                    Bmi = HealthMetric.CalculateBmi(m.WeightKg, client.HeightCm)
                }).ToList(),
                CaloriesBurned = await Context.WorkoutLogs
                    .Where(w => w.ClientId == clientId && w.LoggedAt >= from)
                    .GroupBy(w => w.LoggedAt.Date)
                    .Select(g => new DailyTotalViewModel { Date = g.Key, Total = g.Sum(w => w.CaloriesBurned) })
                    .OrderBy(d => d.Date)
                    .ToListAsync(),
                CaloriesConsumed = await Context.MealLogs
                    .Where(m => m.ClientId == clientId && m.LoggedAt >= from)
                    .GroupBy(m => m.LoggedAt.Date)
                    .Select(g => new DailyTotalViewModel { Date = g.Key, Total = g.Sum(m => m.Calories) })
                    .OrderBy(d => d.Date)
                    .ToListAsync()
            };
            return Json(model);
        }

        [HttpGet]
        public IActionResult DeleteAccount()
        {
            return View(new DeleteAccountViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await UserManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!await UserManager.CheckPasswordAsync(user, model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "Incorrect password.");
                return View(model);
            }

            IdentityResult result;
            try { result = await UserManager.DeleteAsync(user); }
            catch (DbUpdateException exception)
            {
                _logger.LogError(exception, "Client {ClientId} could not delete their account", user.Id);
                result = IdentityResult.Failed();
            }
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Your account could not be deleted. Please try again.");
                return View(model);
            }

            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            _logger.LogInformation("Client {ClientId} deleted their own account", user.Id);
            TempData["Success"] = "Your account and data have been deleted.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Export(string format = "csv", bool all = false)
        {
            var clientId = CurrentUserId;
            var client = await Context.Clients
                .Include(c => c.HealthMetrics)
                .Include(c => c.WorkoutLogs)
                .Include(c => c.MealLogs)
                .AsSplitQuery()
                .FirstAsync(c => c.Id == clientId);

            var sections = new List<ReportSection>
            {
                new ReportSection("Health Metrics", new[] { "Date", "Weight (kg)", "BMI", "Category" },
                    client.HealthMetrics.OrderBy(m => m.RecordedAt)
                        .Select(m => new object?[] { m.RecordedAt, m.WeightKg, m.Bmi, m.BmiCategory })),
                new ReportSection("Workouts", new[] { "Date", "Activity", "Duration (min)", "Calories Burned" },
                    client.WorkoutLogs.OrderBy(w => w.LoggedAt)
                        .Select(w => new object?[] { w.LoggedAt, w.Activity, w.DurationMinutes, w.CaloriesBurned })),
                new ReportSection("Meals", new[] { "Date", "Meal", "Calories", "Food Items" },
                    client.MealLogs.OrderBy(m => m.LoggedAt)
                        .Select(m => new object?[] { m.LoggedAt, m.MealType, m.Calories, m.FoodItems }))
            };

            if (all)
            {
                var appointments = await Context.Appointments.AsNoTracking()
                    .Where(a => a.ClientId == clientId)
                    .Select(a => new { a.Slot.StartUtc, Provider = a.Slot.Provider.Name, a.Status, a.Notes })
                    .OrderBy(a => a.StartUtc).ToListAsync();
                var feedback = await Context.TrainerFeedbacks.AsNoTracking()
                    .Where(f => f.ClientId == clientId)
                    .Select(f => new { f.CreatedAtUtc, Trainer = f.Trainer!.Name, f.Category, f.Title, f.Comments, f.ClientReply })
                    .OrderBy(f => f.CreatedAtUtc).ToListAsync();
                var notifications = await Context.Notifications.AsNoTracking()
                    .Where(n => n.PersonId == clientId)
                    .OrderBy(n => n.CreatedAt).ToListAsync();
                sections.InsertRange(0, new[]
                {
                    new ReportSection("Profile", new[] { "Name", "Email", "Date of Birth", "Height (cm)", "Fitness Goal" },
                        new[] { new object?[] { client.Name, client.Email, client.DateOfBirth, client.HeightCm, client.FitnessGoal } })
                });
                sections.Add(new ReportSection("Appointments", new[] { "Start", "Provider", "Status", "Notes" },
                    appointments.Select(a => new object?[] { _appointmentTime.ToLocal(a.StartUtc), a.Provider, a.Status, a.Notes })));
                sections.Add(new ReportSection("Trainer Feedback", new[] { "Date", "Trainer", "Category", "Title", "Comments", "Your Reply" },
                    feedback.Select(f => new object?[] { f.CreatedAtUtc, f.Trainer, f.Category, f.Title, f.Comments, f.ClientReply })));
                sections.Add(new ReportSection("Notifications", new[] { "Date", "Type", "Message" },
                    notifications.Select(n => new object?[] { n.CreatedAt, n.Type, n.Message })));
            }

            var fileName = $"{(all ? "HealthTrack_MyData" : "HealthTrack_Progress")}_{DateTime.Now:yyyyMMdd}";
            _logger.LogInformation("Client {ClientId} exported {Scope} as {Format}", clientId, all ? "all data" : "progress", format);
            await AuditAsync("Exported", all ? "AllMyData" : "ProgressReport", clientId, format);
            if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                return File(_exportService.ToPdf($"{(all ? "My Data" : "Progress Report")} - {client.Name}", sections), "application/pdf", fileName + ".pdf");
            }
            if (format.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return File(_exportService.ToXlsx($"{(all ? "My Data" : "Progress Report")} - {client.Name}", sections), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName + ".xlsx");
            }
            return File(_exportService.ToCsv(sections), "text/csv", fileName + ".csv");
        }
    }
}
