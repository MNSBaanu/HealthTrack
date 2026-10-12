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

        public AdminController(ILogger<AdminController> logger, HealthTrackDbContext context, UserManager<Person> userManager,
            INotificationService notificationService, IExportService exportService)
            : base(context, userManager)
        {
            _logger = logger;
            _notificationService = notificationService;
            _exportService = exportService;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTimeOffset.Now;
            var weekStart = DateTime.Today.AddDays(-6);

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

            var dates = Enumerable.Range(0, days).Select(i => from.AddDays(i)).ToList();
            var model = new UsageDataViewModel
            {
                Labels = dates.Select(d => d.ToString("yyyy-MM-dd")).ToList(),
                Workouts = dates.Select(d => workouts.GetValueOrDefault(d)).ToList(),
                Meals = dates.Select(d => meals.GetValueOrDefault(d)).ToList(),
                Metrics = dates.Select(d => metrics.GetValueOrDefault(d)).ToList(),
                Registrations = dates.Select(d => registrations.GetValueOrDefault(d)).ToList()
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
            return client == null ? NotFound() : View(client);
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

        public async Task<IActionResult> Reports()
        {
            ViewData["PopularActivities"] = await GetPopularActivitiesAsync(10);
            return View(await GetClientPerformanceAsync());
        }

        [HttpGet]
        public async Task<IActionResult> ExportReport(string format = "csv")
        {
            var performance = await GetClientPerformanceAsync();
            var activities = await GetPopularActivitiesAsync(10);

            var sections = new List<ReportSection>
            {
                new ReportSection("Client Performance",
                    new[] { "Name", "Email", "Start Weight (kg)", "Current Weight (kg)", "Change (kg)", "BMI", "Workouts", "Minutes", "Calories Burned", "Meal Logs", "Last Login" },
                    performance.Select(p => new object?[]
                    {
                        p.Name, p.Email, p.StartWeightKg, p.CurrentWeightKg, p.WeightChangeKg, p.CurrentBmi,
                        p.WorkoutCount, p.TotalMinutes, p.TotalCaloriesBurned, p.MealLogCount, p.LastLoginAt
                    })),
                new ReportSection("Popular Routines",
                    new[] { "Activity", "Sessions", "Clients", "Total Minutes" },
                    activities.Select(a => new object?[] { a.Activity, a.Sessions, a.ClientCount, a.TotalMinutes })),
                new ReportSection("Engagement",
                    new[] { "Metric", "Value" },
                    new[]
                    {
                        new object?[] { "Total clients", performance.Count },
                        new object?[] { "Clients active in last 7 days", performance.Count(p => p.LastLoginAt >= DateTime.Today.AddDays(-6)) },
                        new object?[] { "Clients with at least one workout", performance.Count(p => p.WorkoutCount > 0) },
                        new object?[] { "Average workouts per client", performance.Count == 0 ? 0 : Math.Round(performance.Average(p => p.WorkoutCount), 1) }
                    })
            };

            var fileName = $"HealthTrack_Report_{DateTime.Now:yyyyMMdd}";
            _logger.LogInformation("Admin {AdminId} exported report as {Format}", CurrentUserId, format);
            if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                return File(_exportService.ToPdf("HealthTrack - Client Performance Report", sections), "application/pdf", fileName + ".pdf");
            }
            return File(_exportService.ToCsv(sections), "text/csv", fileName + ".csv");
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
            if (model.ScheduledFor.HasValue && model.ScheduledFor < DateTime.Now)
            {
                ModelState.AddModelError(nameof(model.ScheduledFor), "Scheduled time must be in the future.");
            }
            if (!string.IsNullOrEmpty(model.RecipientId) && !await Context.Clients.AnyAsync(u => u.Id == model.RecipientId))
            {
                ModelState.AddModelError(nameof(model.RecipientId), "Choose a valid Client recipient.");
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

        private Task<List<ActivityStatViewModel>> GetPopularActivitiesAsync(int count)
        {
            return Context.WorkoutLogs
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

        private Task<List<ClientPerformanceViewModel>> GetClientPerformanceAsync()
        {
            return Context.Clients
                .Select(c => new ClientPerformanceViewModel
                {
                    ClientId = c.Id,
                    Name = c.Name,
                    Email = c.Email ?? string.Empty,
                    HeightCm = c.HeightCm,
                    StartWeightKg = c.HealthMetrics.OrderBy(m => m.RecordedAt).Select(m => (double?)m.WeightKg).FirstOrDefault(),
                    CurrentWeightKg = c.HealthMetrics.OrderByDescending(m => m.RecordedAt).Select(m => (double?)m.WeightKg).FirstOrDefault(),
                    WorkoutCount = c.WorkoutLogs.Count(),
                    TotalMinutes = c.WorkoutLogs.Sum(w => w.DurationMinutes),
                    TotalCaloriesBurned = c.WorkoutLogs.Sum(w => w.CaloriesBurned),
                    MealLogCount = c.MealLogs.Count(),
                    LastLoginAt = c.LastLoginAt
                })
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        private static async Task<Dictionary<DateTime, int>> CountPerDayAsync(IQueryable<DateTime> dates)
        {
            return await dates
                .GroupBy(d => d.Date)
                .Select(g => new DailyTotalViewModel { Date = g.Key, Total = g.Count() })
                .ToDictionaryAsync(d => d.Date, d => d.Total);
        }

        private async Task LoadRecipientsAsync()
        {
            ViewData["Recipients"] = await Context.Clients
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id, Text = c.Name + " (" + c.Email + ")" })
                .ToListAsync();
        }
    }
}
