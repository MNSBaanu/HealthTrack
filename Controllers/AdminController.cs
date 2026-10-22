using System.Linq.Expressions;
using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using HealthTrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminController : BaseController
    {
        private readonly ILogger<AdminController> _logger;
        private readonly INotificationService _notificationService;
        private readonly IExportService _exportService;
        private readonly AppointmentTime _time;
        private readonly TimeProvider _clock;

        public AdminController(ILogger<AdminController> logger, HealthTrackDbContext context, UserManager<Person> userManager,
            INotificationService notificationService, IExportService exportService, AppointmentTime time, TimeProvider clock)
            : base(context, userManager)
        {
            _logger = logger;
            _notificationService = notificationService;
            _exportService = exportService;
            _time = time;
            _clock = clock;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTimeOffset.Now;
            var weekStart = DateTime.Today.AddDays(-6);
            var weekStartUtc = weekStart.ToUniversalTime();

            var model = new AdminDashboardViewModel
            {
                TotalUsers = await Context.Users.CountAsync(),
                TotalClients = await Context.Clients.CountAsync(),
                TotalAdmins = await Context.Admins.CountAsync(),
                TotalTrainers = await Context.Trainers.CountAsync(),
                AssignedClients = await Context.ClientTrainerAssignments.CountAsync(),
                UnassignedClients = await Context.Clients.CountAsync(c => c.TrainerAssignment == null),
                RecentAssignments = await Context.ClientTrainerAssignments.AsNoTracking()
                    .OrderByDescending(a => a.AssignedAtUtc).Take(5)
                    .Select(a => new AssignmentRowViewModel
                    {
                        ClientId = a.ClientId, ClientName = a.Client.Name,
                        TrainerId = a.TrainerId, TrainerName = a.Trainer.Name,
                        AssignedAtUtc = a.AssignedAtUtc
                    }).ToListAsync(),
                NewUsersThisWeek = await Context.Users.CountAsync(u => u.CreatedAt >= weekStart),
                ActiveUsersThisWeek = await Context.Users.CountAsync(u => u.LastLoginAt >= weekStart),
                LockedUsers = await Context.Users.CountAsync(u => u.LockoutEnd > now),
                WorkoutLogsThisWeek = await Context.WorkoutLogs.CountAsync(w => w.LoggedAt >= weekStart),
                MealLogsThisWeek = await Context.MealLogs.CountAsync(m => m.LoggedAt >= weekStart),
                AppointmentsThisWeek = await Context.Appointments.CountAsync(a => a.RequestedAtUtc >= weekStartUtc),
                NotificationsThisWeek = await Context.Notifications.CountAsync(n => n.CreatedAt >= weekStart),
                TrainerPlansThisWeek = await Context.WorkoutPlans.CountAsync(p => p.CreatedAtUtc >= weekStartUtc)
                    + await Context.DietPlans.CountAsync(p => p.CreatedAtUtc >= weekStartUtc),
                FeedbackThisWeek = await Context.TrainerFeedbacks.CountAsync(f => f.CreatedAtUtc >= weekStartUtc),
                MetricsThisWeek = await Context.HealthMetrics.CountAsync(m => m.RecordedAt >= weekStart),
                PopularActivities = await GetPopularActivitiesAsync(5),
                RecentUsers = await QueryUserSummaries()
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> UsageData(int days = 14)
        {
            days = Math.Clamp(days, 7, 90);
            var from = DateTime.Today.AddDays(-(days - 1));

            var workouts = await CountPerDayAsync(Context.WorkoutLogs.Where(w => w.LoggedAt >= from).Select(w => w.LoggedAt));
            var meals = await CountPerDayAsync(Context.MealLogs.Where(m => m.LoggedAt >= from).Select(m => m.LoggedAt));
            var metrics = await CountPerDayAsync(Context.HealthMetrics.Where(m => m.RecordedAt >= from).Select(m => m.RecordedAt));
            var registrations = await CountPerDayAsync(Context.Users.Where(u => u.CreatedAt >= from).Select(u => u.CreatedAt));
            var fromUtc = from.ToUniversalTime();
            var appointments = await CountPerDayAsync(Context.Appointments.Where(a => a.RequestedAtUtc >= fromUtc).Select(a => a.RequestedAtUtc));
            var notifications = await CountPerDayAsync(Context.Notifications.Where(n => n.CreatedAt >= from).Select(n => n.CreatedAt));
            var workoutPlans = await CountPerDayAsync(Context.WorkoutPlans.Where(p => p.CreatedAtUtc >= fromUtc).Select(p => p.CreatedAtUtc));
            var dietPlans = await CountPerDayAsync(Context.DietPlans.Where(p => p.CreatedAtUtc >= fromUtc).Select(p => p.CreatedAtUtc));
            var feedback = await CountPerDayAsync(Context.TrainerFeedbacks.Where(f => f.CreatedAtUtc >= fromUtc).Select(f => f.CreatedAtUtc));

            var dates = Enumerable.Range(0, days).Select(i => from.AddDays(i)).ToList();
            var model = new UsageDataViewModel
            {
                Labels = dates.Select(d => d.ToString("yyyy-MM-dd")).ToList(),
                Workouts = dates.Select(d => workouts.GetValueOrDefault(d)).ToList(),
                Meals = dates.Select(d => meals.GetValueOrDefault(d)).ToList(),
                Metrics = dates.Select(d => metrics.GetValueOrDefault(d)).ToList(),
                Registrations = dates.Select(d => registrations.GetValueOrDefault(d)).ToList(),
                Appointments = dates.Select(d => appointments.GetValueOrDefault(d)).ToList(),
                Notifications = dates.Select(d => notifications.GetValueOrDefault(d)).ToList(),
                Plans = dates.Select(d => workoutPlans.GetValueOrDefault(d) + dietPlans.GetValueOrDefault(d)).ToList(),
                Feedback = dates.Select(d => feedback.GetValueOrDefault(d)).ToList()
            };
            return Json(model);
        }

        public async Task<IActionResult> Users(string? search, string? role)
        {
            var query = QueryUserSummaries();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.Name.Contains(search) || u.Email.Contains(search));
            }
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }

            ViewData["Search"] = search;
            ViewData["Role"] = role;
            return View(await query.OrderBy(u => u.Name).ToListAsync());
        }

        public async Task<IActionResult> ClientDetails(string id)
        {
            var client = await Context.Clients
                .Include(c => c.HealthMetrics.OrderByDescending(m => m.RecordedAt))
                .Include(c => c.WorkoutLogs.OrderByDescending(w => w.LoggedAt))
                .Include(c => c.MealLogs.OrderByDescending(m => m.LoggedAt))
                .Include(c => c.TrainerAssignment).ThenInclude(a => a!.Trainer)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id);
            if (client == null) return NotFound();
            await AuditAsync("Viewed", "ClientDetails", id);
            return View(client);
        }

        public async Task<IActionResult> LoginHistory(string id)
        {
            var person = await UserManager.FindByIdAsync(id);
            if (person == null) return NotFound();
            ViewData["Person"] = person;
            return View(await Context.LoginHistories.Where(l => l.PersonId == id)
                .OrderByDescending(l => l.LoggedInAt).Take(100).ToListAsync());
        }

        [HttpGet]
        public IActionResult CreateAdmin()
        {
            return View(new CreateAdminViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> CreateAdmin(CreateAdminViewModel model)
        {
            ValidatePastDate(model.DateOfBirth, nameof(model.DateOfBirth));
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var admin = new Admin
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                Name = model.Name.Trim(),
                DateOfBirth = model.DateOfBirth!.Value
            };

            var result = await UserManager.CreateAsync(admin, model.Password);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            await UserManager.AddToRoleAsync(admin, AppRoles.Admin);
            _logger.LogInformation("Admin {AdminId} created administrator {Email}", CurrentUserId, admin.Email);
            TempData["Success"] = $"Administrator {admin.Name} created.";
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public IActionResult CreateTrainer()
        {
            return View(new CreateTrainerViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> CreateTrainer(CreateTrainerViewModel model)
        {
            ValidatePastDate(model.DateOfBirth, nameof(model.DateOfBirth));
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var trainer = new Trainer
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                Name = model.Name.Trim(),
                DateOfBirth = model.DateOfBirth!.Value
            };

            var result = await UserManager.CreateAsync(trainer, model.Password);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            await UserManager.AddToRoleAsync(trainer, AppRoles.Trainer);

            var provider = new ConsultationProvider
            {
                Name = trainer.Name,
                Specialty = string.IsNullOrWhiteSpace(model.Specialty) ? "Fitness & Strength Coaching" : model.Specialty.Trim(),
                Kind = ConsultationProviderKind.Trainer,
                IsActive = true,
                PersonId = trainer.Id
            };
            Context.ConsultationProviders.Add(provider);
            await Context.SaveChangesAsync();

            _logger.LogInformation("Admin {AdminId} created trainer {Email}", CurrentUserId, trainer.Email);
            TempData["Success"] = $"Trainer {trainer.Name} created successfully.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLock(string id)
        {
            if (id == CurrentUserId)
            {
                TempData["Error"] = "You cannot lock your own account.";
                return RedirectToAction(nameof(Users));
            }

            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var isLocked = await UserManager.IsLockedOutAsync(user);
            await UserManager.SetLockoutEnabledAsync(user, true);
            await UserManager.SetLockoutEndDateAsync(user, isLocked ? null : DateTimeOffset.MaxValue);
            await UserManager.UpdateSecurityStampAsync(user);

            _logger.LogInformation("Admin {AdminId} {Action} user {UserId}", CurrentUserId, isLocked ? "unlocked" : "locked", id);
            TempData["Success"] = $"{user.Name} has been {(isLocked ? "unlocked" : "locked")}.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (id == CurrentUserId)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (user is Trainer && await Context.ClientTrainerAssignments.AnyAsync(a => a.TrainerId == id))
            {
                TempData["Error"] = "Reassign or remove this Trainer's Client assignments before deleting the account.";
                return RedirectToAction(nameof(Users));
            }
            IdentityResult result;
            try { result = await UserManager.DeleteAsync(user); }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 })
            {
                TempData["Error"] = "This account has linked records. Remove its current assignments or retain the account for existing consultations.";
                return RedirectToAction(nameof(Users));
            }
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Users));
            }

            _logger.LogInformation("Admin {AdminId} deleted user {UserId}", CurrentUserId, id);
            TempData["Success"] = $"{user.Name} has been deleted.";
            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> Reports(DateTime? from, DateTime? to)
        {
            if (from > to) (from, to) = (to, from);
            ViewData["From"] = from;
            ViewData["To"] = to;
            ViewData["PopularActivities"] = await GetPopularActivitiesAsync(10, from, to);
            var (planExercises, topPlans) = await GetPlanRoutinesAsync(10, from, to);
            ViewData["PlanExercises"] = planExercises;
            ViewData["TopPlans"] = topPlans;
            return View(await GetClientPerformanceAsync(from, to));
        }

        [HttpGet]
        public async Task<IActionResult> ExportReport(string format = "csv", DateTime? from = null, DateTime? to = null)
        {
            if (from > to) (from, to) = (to, from);
            var performance = await GetClientPerformanceAsync(from, to);
            var activities = await GetPopularActivitiesAsync(10, from, to);
            var (planExercises, topPlans) = await GetPlanRoutinesAsync(10, from, to);

            var sections = new List<ReportSection>
            {
                new ReportSection("Client Performance",
                    new[] { "Name", "Email", "Start Weight (kg)", "Current Weight (kg)", "Change (kg)", "BMI", "Workouts", "Minutes", "Calories Burned", "Meal Logs", "Weight Logs", "Appointments", "Active Days", "Last Login" },
                    performance.Select(p => new object?[]
                    {
                        p.Name, p.Email, p.StartWeightKg, p.CurrentWeightKg, p.WeightChangeKg, p.CurrentBmi,
                        p.WorkoutCount, p.TotalMinutes, p.TotalCaloriesBurned, p.MealLogCount, p.WeightLogCount, p.AppointmentCount, p.ActiveDays, p.LastLoginAt
                    })),
                new ReportSection("Popular Routines",
                    new[] { "Activity", "Sessions", "Clients", "Total Minutes" },
                    activities.Select(a => new object?[] { a.Activity, a.Sessions, a.ClientCount, a.TotalMinutes })),
                new ReportSection("Popular Plan Exercises",
                    new[] { "Exercise", "Plans", "Clients", "Completions" },
                    planExercises.Select(e => new object?[] { e.Exercise, e.PlanCount, e.ClientCount, e.Completions })),
                new ReportSection("Top Trainer Plans",
                    new[] { "Plan", "Trainer", "Client", "Exercises", "Completions" },
                    topPlans.Select(p => new object?[] { p.Title, p.Trainer, p.Client, p.ItemCount, p.Completions })),
                new ReportSection("Engagement",
                    new[] { "Metric", "Value" },
                    new[]
                    {
                        new object?[] { "Total clients", performance.Count },
                        new object?[] { "Clients active in last 7 days", performance.Count(p => p.LastLoginAt >= DateTime.Today.AddDays(-6)) },
                        new object?[] { "Clients with at least one workout", performance.Count(p => p.WorkoutCount > 0) },
                        new object?[] { "Average workouts per client", performance.Count == 0 ? 0 : Math.Round(performance.Average(p => p.WorkoutCount), 1) },
                        new object?[] { "Clients who logged a meal", performance.Count(p => p.MealLogCount > 0) },
                        new object?[] { "Clients who logged their weight", performance.Count(p => p.WeightLogCount > 0) },
                        new object?[] { "Appointments requested", performance.Sum(p => p.AppointmentCount) },
                        new object?[] { "Average active days per client", performance.Count == 0 ? 0 : Math.Round(performance.Average(p => p.ActiveDays), 1) }
                    })
            };

            var fileName = $"HealthTrack_Report_{DateTime.Now:yyyyMMdd}";
            _logger.LogInformation("Admin {AdminId} exported report as {Format}", CurrentUserId, format);
            await AuditAsync("Exported", "ClientPerformanceReport", null, format);
            if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                return File(_exportService.ToPdf("HealthTrack - Client Performance Report", sections), "application/pdf", fileName + ".pdf");
            }
            return File(_exportService.ToCsv(sections), "text/csv", fileName + ".csv");
        }

        [HttpGet]
        public async Task<IActionResult> AuditLog(string? actor, string? action, DateTime? from, DateTime? to)
        {
            if (from > to) (from, to) = (to, from);
            ViewData["Actor"] = actor;
            ViewData["Action"] = action;
            ViewData["From"] = from;
            ViewData["To"] = to;
            var logs = Context.AuditLogs.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(actor)) logs = logs.Where(l => l.ActorName.Contains(actor.Trim()));
            if (!string.IsNullOrWhiteSpace(action)) logs = logs.Where(l => l.Action == action);
            if (from.HasValue) logs = logs.Where(l => l.CreatedAtUtc >= from.Value.Date.ToUniversalTime());
            if (to.HasValue) logs = logs.Where(l => l.CreatedAtUtc < to.Value.Date.AddDays(1).ToUniversalTime());
            var entries = await logs.OrderByDescending(l => l.CreatedAtUtc).Take(300).ToListAsync();
            var subjectIds = entries.Where(l => l.SubjectClientId != null).Select(l => l.SubjectClientId!).Distinct().ToList();
            ViewData["SubjectNames"] = await Context.Clients.Where(c => subjectIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);
            return View(entries);
        }

        [HttpGet]
        public async Task<IActionResult> ChatQuestions()
        {
            return View(await Context.ChatQuestions.AsNoTracking()
                .OrderByDescending(q => q.CreatedAtUtc).Take(500).ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> SendNotification()
        {
            await LoadRecipientsAsync();
            return View(new SendNotificationViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> SendNotification(SendNotificationViewModel model)
        {
            if (model.ScheduledFor.HasValue && model.ScheduledFor <= _time.ToLocal(_clock.GetUtcNow().UtcDateTime))
            {
                ModelState.AddModelError(nameof(model.ScheduledFor), "Scheduled time must be in the future.");
            }
            var allTrainers = model.RecipientId == AllTrainersRecipient;
            if (!string.IsNullOrEmpty(model.RecipientId) && !allTrainers
                && !await Context.Clients.AnyAsync(u => u.Id == model.RecipientId)
                && !await Context.Trainers.AnyAsync(u => u.Id == model.RecipientId))
            {
                ModelState.AddModelError(nameof(model.RecipientId), "Choose a valid recipient.");
            }
            if (!ModelState.IsValid)
            {
                await LoadRecipientsAsync();
                return View(model);
            }

            if (string.IsNullOrEmpty(model.RecipientId))
            {
                await _notificationService.SendToAllClientsAsync(model.Message.Trim(), model.Type, model.ScheduledFor);
            }
            else if (allTrainers)
            {
                await _notificationService.SendToAllTrainersAsync(model.Message.Trim(), model.Type, model.ScheduledFor);
            }
            else
            {
                await _notificationService.SendAsync(model.RecipientId, model.Message.Trim(), model.Type, model.ScheduledFor);
            }

            TempData["Success"] = "Notification sent.";
            return RedirectToAction(nameof(Index));
        }

        private IQueryable<UserSummaryViewModel> QueryUserSummaries()
        {
            var now = DateTimeOffset.Now;
            return Context.Users.Select(u => new UserSummaryViewModel
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email ?? string.Empty,
                Role = EF.Property<string>(u, "Role"),
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                IsLocked = u.LockoutEnd != null && u.LockoutEnd > now
            });
        }

        private Task<List<ActivityStatViewModel>> GetPopularActivitiesAsync(int count, DateTime? from = null, DateTime? to = null)
        {
            var start = from?.Date;
            var end = to?.Date.AddDays(1);
            return Context.WorkoutLogs
                .Where(w => (start == null || w.LoggedAt >= start) && (end == null || w.LoggedAt < end))
                .GroupBy(w => w.Activity)
                .Select(g => new ActivityStatViewModel
                {
                    Activity = g.Key,
                    Sessions = g.Count(),
                    ClientCount = g.Select(w => w.ClientId).Distinct().Count(),
                    TotalMinutes = g.Sum(w => w.DurationMinutes)
                })
                .OrderByDescending(a => a.Sessions)
                .Take(count)
                .ToListAsync();
        }

        private async Task<(List<PlanExerciseStatViewModel> Exercises, List<PlanStatViewModel> Plans)> GetPlanRoutinesAsync(int count, DateTime? from, DateTime? to)
        {
            var start = from?.Date;
            var end = to?.Date.AddDays(1);
            var completions = await Context.WorkoutLogs
                .Where(w => w.WorkoutPlanItemId != null
                    && (start == null || w.LoggedAt >= start) && (end == null || w.LoggedAt < end))
                .GroupBy(w => w.WorkoutPlanItemId!.Value)
                .Select(g => new { ItemId = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.ItemId, x => x.Total);
            var items = await Context.WorkoutPlanItems.AsNoTracking()
                .Select(i => new
                {
                    i.Id,
                    i.ExerciseName,
                    i.WorkoutPlanId,
                    i.WorkoutPlan!.Title,
                    i.WorkoutPlan.ClientId,
                    ClientName = i.WorkoutPlan.Client!.Name,
                    TrainerName = i.WorkoutPlan.Trainer!.Name
                })
                .ToListAsync();

            var exercises = items
                .GroupBy(i => i.ExerciseName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new PlanExerciseStatViewModel
                {
                    Exercise = g.Key,
                    PlanCount = g.Select(i => i.WorkoutPlanId).Distinct().Count(),
                    ClientCount = g.Select(i => i.ClientId).Distinct().Count(),
                    Completions = g.Sum(i => completions.GetValueOrDefault(i.Id))
                })
                .OrderByDescending(e => e.Completions).ThenByDescending(e => e.PlanCount).ThenBy(e => e.Exercise)
                .Take(count).ToList();
            var plans = items
                .GroupBy(i => i.WorkoutPlanId)
                .Select(g => new PlanStatViewModel
                {
                    Title = g.First().Title,
                    Trainer = g.First().TrainerName,
                    Client = g.First().ClientName,
                    ItemCount = g.Count(),
                    Completions = g.Sum(i => completions.GetValueOrDefault(i.Id))
                })
                .OrderByDescending(p => p.Completions).ThenBy(p => p.Title)
                .Take(count).ToList();
            return (exercises, plans);
        }

        private async Task<List<ClientPerformanceViewModel>> GetClientPerformanceAsync(DateTime? from = null, DateTime? to = null)
        {
            var start = from?.Date;
            var end = to?.Date.AddDays(1);
            var startUtc = start?.ToUniversalTime();
            var endUtc = end?.ToUniversalTime();
            Expression<Func<WorkoutLog, bool>> workoutInRange = w => (start == null || w.LoggedAt >= start) && (end == null || w.LoggedAt < end);
            Expression<Func<MealLog, bool>> mealInRange = m => (start == null || m.LoggedAt >= start) && (end == null || m.LoggedAt < end);
            Expression<Func<HealthMetric, bool>> metricInRange = m => (start == null || m.RecordedAt >= start) && (end == null || m.RecordedAt < end);

            var clients = await Context.Clients
                .Select(c => new ClientPerformanceViewModel
                {
                    ClientId = c.Id,
                    Name = c.Name,
                    Email = c.Email ?? string.Empty,
                    HeightCm = c.HeightCm,
                    StartWeightKg = c.HealthMetrics.AsQueryable().Where(metricInRange).OrderBy(m => m.RecordedAt).Select(m => (double?)m.WeightKg).FirstOrDefault(),
                    CurrentWeightKg = c.HealthMetrics.AsQueryable().Where(metricInRange).OrderByDescending(m => m.RecordedAt).Select(m => (double?)m.WeightKg).FirstOrDefault(),
                    WeightLogCount = c.HealthMetrics.AsQueryable().Count(metricInRange),
                    WorkoutCount = c.WorkoutLogs.AsQueryable().Count(workoutInRange),
                    TotalMinutes = c.WorkoutLogs.AsQueryable().Where(workoutInRange).Sum(w => w.DurationMinutes),
                    TotalCaloriesBurned = c.WorkoutLogs.AsQueryable().Where(workoutInRange).Sum(w => w.CaloriesBurned),
                    MealLogCount = c.MealLogs.AsQueryable().Count(mealInRange),
                    LastLoginAt = c.LastLoginAt
                })
                .OrderBy(c => c.Name)
                .ToListAsync();

            var appointments = await Context.Appointments
                .Where(a => (startUtc == null || a.RequestedAtUtc >= startUtc) && (endUtc == null || a.RequestedAtUtc < endUtc))
                .GroupBy(a => a.ClientId)
                .Select(g => new { ClientId = g.Key, Total = g.Count() })
                .ToDictionaryAsync(a => a.ClientId, a => a.Total);
            var activeDays = (await Context.WorkoutLogs.Where(workoutInRange).Select(w => new { w.ClientId, w.LoggedAt.Date }).Distinct().ToListAsync())
                .Concat(await Context.MealLogs.Where(mealInRange).Select(m => new { m.ClientId, m.LoggedAt.Date }).Distinct().ToListAsync())
                .Concat(await Context.HealthMetrics.Where(metricInRange).Select(m => new { m.ClientId, m.RecordedAt.Date }).Distinct().ToListAsync())
                .Distinct()
                .GroupBy(d => d.ClientId)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var client in clients)
            {
                client.AppointmentCount = appointments.GetValueOrDefault(client.ClientId);
                client.ActiveDays = activeDays.GetValueOrDefault(client.ClientId);
            }
            return clients;
        }

        private static async Task<Dictionary<DateTime, int>> CountPerDayAsync(IQueryable<DateTime> dates)
        {
            return await dates
                .GroupBy(d => d.Date)
                .Select(g => new DailyTotalViewModel { Date = g.Key, Total = g.Count() })
                .ToDictionaryAsync(d => d.Date, d => d.Total);
        }

        private const string AllTrainersRecipient = "all-trainers";

        private async Task LoadRecipientsAsync()
        {
            var clients = new SelectListGroup { Name = "Clients" };
            var trainers = new SelectListGroup { Name = "Trainers" };
            var recipients = await Context.Clients
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id, Text = c.Name + " (" + c.Email + ")", Group = clients })
                .ToListAsync();
            recipients.AddRange(await Context.Trainers
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id, Text = t.Name + " (" + t.Email + ")", Group = trainers })
                .ToListAsync());
            ViewData["Recipients"] = recipients;
        }

        public async Task<IActionResult> Providers()
        {
            var providers = await Context.ConsultationProviders.AsNoTracking()
                .Where(p => p.Kind == ConsultationProviderKind.HealthcareProfessional)
                .OrderByDescending(p => p.IsActive).ThenBy(p => p.Name)
                .ToListAsync();
            return View(providers);
        }

        [HttpGet]
        public IActionResult CreateProvider() => View("ProviderForm", new ProviderForm());

        [HttpPost]
        public async Task<IActionResult> CreateProvider(ProviderForm model)
        {
            if (!ModelState.IsValid)
            {
                return View("ProviderForm", model);
            }

            Context.ConsultationProviders.Add(new ConsultationProvider
            {
                Name = model.Name.Trim(),
                Specialty = string.IsNullOrWhiteSpace(model.Specialty) ? null : model.Specialty.Trim(),
                Kind = ConsultationProviderKind.HealthcareProfessional,
                IsActive = true
            });
            await Context.SaveChangesAsync();
            _logger.LogInformation("Admin {AdminId} created healthcare professional {Name}", CurrentUserId, model.Name);
            TempData["Success"] = $"{model.Name.Trim()} has been added.";
            return RedirectToAction(nameof(Providers));
        }

        private Task<ConsultationProvider?> FindProfessionalAsync(int id) =>
            Context.ConsultationProviders.FirstOrDefaultAsync(p => p.Id == id && p.Kind == ConsultationProviderKind.HealthcareProfessional);

        [HttpGet]
        public async Task<IActionResult> EditProvider(int id)
        {
            var provider = await FindProfessionalAsync(id);
            if (provider == null)
            {
                return NotFound();
            }

            return View("ProviderForm", new ProviderForm { Id = provider.Id, Name = provider.Name, Specialty = provider.Specialty });
        }

        [HttpPost]
        public async Task<IActionResult> EditProvider(int id, ProviderForm model)
        {
            var provider = await FindProfessionalAsync(id);
            if (provider == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.Id = id;
                return View("ProviderForm", model);
            }

            provider.Name = model.Name.Trim();
            provider.Specialty = string.IsNullOrWhiteSpace(model.Specialty) ? null : model.Specialty.Trim();
            await Context.SaveChangesAsync();
            TempData["Success"] = $"{provider.Name} has been updated.";
            return RedirectToAction(nameof(Providers));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleProvider(int id)
        {
            var provider = await FindProfessionalAsync(id);
            if (provider == null)
            {
                return NotFound();
            }

            provider.IsActive = !provider.IsActive;
            await Context.SaveChangesAsync();
            _logger.LogInformation("Admin {AdminId} {Action} provider {ProviderId}", CurrentUserId, provider.IsActive ? "activated" : "deactivated", id);
            TempData["Success"] = $"{provider.Name} has been {(provider.IsActive ? "turned on" : "turned off")}.";
            return RedirectToAction(nameof(Providers));
        }

        [HttpGet]
        public async Task<IActionResult> ProviderSlots(int id)
        {
            var provider = await FindProfessionalAsync(id);
            return provider == null ? NotFound() : View(await LoadProviderSlotsAsync(provider, new AddAvailabilitySlotForm()));
        }

        [HttpPost]
        public async Task<IActionResult> AddProviderSlot(int id, [Bind(Prefix = "Form")] AddAvailabilitySlotForm form)
        {
            var provider = await FindProfessionalAsync(id);
            if (provider == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var nowUtc = _clock.GetUtcNow().UtcDateTime;
                var startUtc = _time.ToUtc(form.StartLocal);
                var endUtc = _time.ToUtc(form.EndLocal);
                if (form.EndLocal <= form.StartLocal)
                {
                    ModelState.AddModelError(string.Empty, "Slot end time must be after start time.");
                }
                else if (startUtc <= nowUtc)
                {
                    ModelState.AddModelError(string.Empty, "Slot start time must be in the future.");
                }
                else if (await Context.AvailabilitySlots.AnyAsync(s => s.ProviderId == provider.Id && s.StartUtc < endUtc && s.EndUtc > startUtc))
                {
                    ModelState.AddModelError(string.Empty, "This slot overlaps an existing slot.");
                }
                else
                {
                    Context.AvailabilitySlots.Add(new AvailabilitySlot { ProviderId = provider.Id, StartUtc = startUtc, EndUtc = endUtc, IsAvailable = true });
                    await Context.SaveChangesAsync();
                    TempData["Success"] = "Availability slot added.";
                    return RedirectToAction(nameof(ProviderSlots), new { id });
                }
            }

            return View(nameof(ProviderSlots), await LoadProviderSlotsAsync(provider, form));
        }

        private async Task<ProviderSlotsViewModel> LoadProviderSlotsAsync(ConsultationProvider provider, AddAvailabilitySlotForm form)
        {
            var nowUtc = _clock.GetUtcNow().UtcDateTime;
            var slots = await Context.AvailabilitySlots.AsNoTracking()
                .Where(s => s.ProviderId == provider.Id && s.EndUtc > nowUtc)
                .OrderBy(s => s.StartUtc)
                .ToListAsync();
            return new ProviderSlotsViewModel
            {
                Provider = provider,
                Form = form,
                TimeZoneLabel = _time.Label,
                Slots = slots.Select(s => new AvailableSlotViewModel(s.Id, provider.Name, _time.ToLocal(s.StartUtc), _time.ToLocal(s.EndUtc))).ToList()
            };
        }
    }
}
