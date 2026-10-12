using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using HealthTrack.Services;
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
            var upcoming = await Context.Appointments.AsNoTracking()
                .Include(a => a.Slot).ThenInclude(s => s.Provider)
                .Where(a => a.ClientId == clientId && a.Slot.StartUtc > nowUtc
                    && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved))
                .OrderBy(a => a.Slot.StartUtc).Take(3).ToListAsync();
            var weekStart = today.AddDays(-6);
            var weekWorkouts = await Context.WorkoutLogs
                .Where(w => w.ClientId == clientId && w.LoggedAt >= weekStart)
                .ToListAsync();

            var model = new ClientDashboardViewModel
            {
                Client = client,
                UpcomingAppointments = upcoming.Select(_appointmentTime.ToViewModel).ToList(),
                AppointmentTimeZone = _appointmentTime.Label,
                LatestMetric = metrics.LastOrDefault(),
                WeightChangeKg = metrics.Count > 1 ? Math.Round(metrics[^1].WeightKg - metrics[0].WeightKg, 1) : null,
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

        public IActionResult Progress()
        {
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
        public async Task<IActionResult> Export(string format = "csv")
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

            var fileName = $"HealthTrack_Progress_{DateTime.Now:yyyyMMdd}";
            _logger.LogInformation("Client {ClientId} exported progress as {Format}", clientId, format);
            if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                return File(_exportService.ToPdf($"Progress Report - {client.Name}", sections), "application/pdf", fileName + ".pdf");
            }
            return File(_exportService.ToCsv(sections), "text/csv", fileName + ".csv");
        }
    }
}
