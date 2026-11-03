using System.Text;
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
        public const string ProviderRole = "Provider";
        public const int InactiveDays = 14;
        private const string ReminderMessage = "We miss you at HealthTrack! Log a workout or meal today to keep your progress on track.";
        private readonly ILogger<AdminController> _logger;
        private readonly INotificationService _notificationService;
        private readonly IExportService _exportService;
        private readonly AppointmentTime _time;
        private readonly TimeProvider _clock;
        private readonly UserImportService _userImport;

        public AdminController(ILogger<AdminController> logger, HealthTrackDbContext context, UserManager<Person> userManager,
            INotificationService notificationService, IExportService exportService, AppointmentTime time, TimeProvider clock,
            UserImportService userImport)
            : base(context, userManager)
        {
            _userImport = userImport;
            _logger = logger;
            _notificationService = notificationService;
            _exportService = exportService;
            _time = time;
            _clock = clock;
        }

        public async Task<IActionResult> Index()
        {
            var now = _clock.GetUtcNow();
            var weekStart = _time.ToLocal(now.UtcDateTime).Date.AddDays(-6);
            var weekStartUtc = _time.ToUtc(weekStart);

            var model = new AdminDashboardViewModel
            {
                TotalUsers = await Context.Users.CountAsync(),
                TotalClients = await Context.Clients.CountAsync(),
                TotalAdmins = await Context.Admins.CountAsync(),
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
                PopularActivities = await ReportData.GetPopularActivitiesAsync(5)
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> UsageData(int days = 14)
        {
            days = Math.Clamp(days, 7, 90);
            var from = _time.ToLocal(_clock.GetUtcNow().UtcDateTime).Date.AddDays(-(days - 1));

            var workouts = await CountPerDayAsync(Context.WorkoutLogs.Where(w => w.LoggedAt >= from).Select(w => w.LoggedAt));
            var meals = await CountPerDayAsync(Context.MealLogs.Where(m => m.LoggedAt >= from).Select(m => m.LoggedAt));
            var metrics = await CountPerDayAsync(Context.HealthMetrics.Where(m => m.RecordedAt >= from).Select(m => m.RecordedAt));
            var registrations = await CountPerDayAsync(Context.Users.Where(u => u.CreatedAt >= from).Select(u => u.CreatedAt));
            var fromUtc = _time.ToUtc(from);
            var appointments = await CountUtcPerDayAsync(Context.Appointments.Where(a => a.RequestedAtUtc >= fromUtc).Select(a => a.RequestedAtUtc));
            var workoutPlans = await CountUtcPerDayAsync(Context.WorkoutPlans.Where(p => p.CreatedAtUtc >= fromUtc).Select(p => p.CreatedAtUtc));
            var dietPlans = await CountUtcPerDayAsync(Context.DietPlans.Where(p => p.CreatedAtUtc >= fromUtc).Select(p => p.CreatedAtUtc));
            var feedback = await CountUtcPerDayAsync(Context.TrainerFeedbacks.Where(f => f.CreatedAtUtc >= fromUtc).Select(f => f.CreatedAtUtc));

            var dates = Enumerable.Range(0, days).Select(i => from.AddDays(i)).ToList();
            var model = new UsageDataViewModel
            {
                Labels = dates.Select(d => d.ToString("yyyy-MM-dd")).ToList(),
                Workouts = dates.Select(d => workouts.GetValueOrDefault(d)).ToList(),
                Meals = dates.Select(d => meals.GetValueOrDefault(d)).ToList(),
                Metrics = dates.Select(d => metrics.GetValueOrDefault(d)).ToList(),
                Registrations = dates.Select(d => registrations.GetValueOrDefault(d)).ToList(),
                Appointments = dates.Select(d => appointments.GetValueOrDefault(d)).ToList(),
                Plans = dates.Select(d => workoutPlans.GetValueOrDefault(d) + dietPlans.GetValueOrDefault(d)).ToList(),
                Feedback = dates.Select(d => feedback.GetValueOrDefault(d)).ToList()
            };
            return Json(model);
        }

        public async Task<IActionResult> Users(string? search, string? role, string? status)
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
            if (status == "active") query = query.Where(u => !u.IsLocked);
            else if (status == "locked") query = query.Where(u => u.IsLocked);
            else if (status == "inactive")
            {
                var cutoff = DateTime.Now.AddDays(-InactiveDays);
                query = query.Where(u => u.Role == AppRoles.Client && (u.LastLoginAt == null || u.LastLoginAt < cutoff));
            }

            var providers = new List<ConsultationProvider>();
            if ((string.IsNullOrWhiteSpace(role) || role == ProviderRole) && status is not ("locked" or "inactive"))
            {
                var providerQuery = Context.ConsultationProviders.AsNoTracking()
                    .Where(p => p.Kind == ConsultationProviderKind.HealthcareProfessional);
                if (!string.IsNullOrWhiteSpace(search))
                {
                    providerQuery = providerQuery.Where(p => p.Name.Contains(search) || (p.Specialty != null && p.Specialty.Contains(search)));
                }
                if (status == "active") providerQuery = providerQuery.Where(p => p.IsActive);
                else if (status == "off") providerQuery = providerQuery.Where(p => !p.IsActive);
                providers = await providerQuery.OrderByDescending(p => p.IsActive).ThenBy(p => p.Name).ToListAsync();
            }

            ViewData["Search"] = search;
            ViewData["Role"] = role;
            ViewData["Status"] = status;
            ViewData["Providers"] = providers;
            var lockedAfter = _clock.GetUtcNow();
            ViewData["Trainers"] = (await Context.Trainers.Where(t => t.LockoutEnd == null || t.LockoutEnd <= lockedAfter)
                .OrderBy(t => t.Name).Select(t => new { t.Id, t.Name, Clients = Context.ClientTrainerAssignments.Count(a => a.TrainerId == t.Id) })
                .ToListAsync()).Select(t => new SelectListItem($"{t.Name} · {t.Clients} {(t.Clients == 1 ? "client" : "clients")}", t.Id)).ToList();
            var users = role == ProviderRole || status == "off" ? new List<UserSummaryViewModel>() : await query.OrderBy(u => u.Name).ToListAsync();
            return View(users);
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

        public async Task<IActionResult> TrainerDetails(string id)
        {
            var trainer = await Context.Trainers
                .Include(t => t.ClientAssignments).ThenInclude(a => a.Client)
                .Include(t => t.WorkoutPlans)
                .Include(t => t.DietPlans)
                .AsSplitQuery()
                .FirstOrDefaultAsync(t => t.Id == id);
            if (trainer == null) return NotFound();

            var provider = await Context.ConsultationProviders
                .FirstOrDefaultAsync(p => p.PersonId == id);

            var appointmentCount = await Context.Appointments
                .CountAsync(a => a.Slot.Provider.PersonId == id);

            ViewData["Provider"] = provider;
            ViewData["AppointmentCount"] = appointmentCount;
            return View(trainer);
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
        public async Task<IActionResult> Ratings()
        {
            return View(await ReportData.GetRatingsAsync(50));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleRating(int id)
        {
            var appointment = await Context.Appointments.SingleOrDefaultAsync(a => a.Id == id && a.Rating != null);
            if (appointment == null) return NotFound();
            appointment.RatingHidden = !appointment.RatingHidden;
            await Context.SaveChangesAsync();
            await AuditAsync(appointment.RatingHidden ? "Hidden" : "Restored", "Rating", appointment.ClientId, $"Appointment {id}");
            TempData["Success"] = appointment.RatingHidden ? "Rating hidden. It no longer counts towards the average." : "Rating restored.";
            return RedirectToAction(nameof(Ratings));
        }

        [HttpGet]
        public async Task<IActionResult> Appointments(string? search, AppointmentStatus? status, DateTime? from, DateTime? to)
        {
            if (from > to) (from, to) = (to, from);
            var query = Context.Appointments.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(a => a.Client.Name.Contains(term) || a.Slot.Provider.Name.Contains(term));
            }
            if (status.HasValue) query = query.Where(a => a.Status == status);
            if (from.HasValue)
            {
                var fromUtc = _time.ToUtc(from.Value.Date);
                query = query.Where(a => a.Slot.StartUtc >= fromUtc);
            }
            if (to.HasValue)
            {
                var toUtc = _time.ToUtc(to.Value.Date.AddDays(1));
                query = query.Where(a => a.Slot.StartUtc < toUtc);
            }

            var rows = await query.OrderByDescending(a => a.Slot.StartUtc).Take(500)
                .Select(a => new { a.Id, a.ClientId, Client = a.Client.Name, Provider = a.Slot.Provider.Name, a.Slot.Provider.Kind, a.Slot.StartUtc, a.Status, a.RequestedAtUtc })
                .ToListAsync();
            ViewData["Search"] = search;
            ViewData["Status"] = status;
            ViewData["NowLocal"] = _time.ToLocal(_clock.GetUtcNow().UtcDateTime);
            ViewData["Limit"] = 500;
            ViewData["From"] = from;
            ViewData["To"] = to;
            return View(rows.Select(a => new AdminAppointmentRow(a.Id, a.ClientId, a.Client, a.Provider, a.Kind,
                _time.ToLocal(a.StartUtc), a.Status, _time.ToLocal(a.RequestedAtUtc))).ToList());
        }

        [HttpPost]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await Context.Appointments.Include(a => a.Slot).ThenInclude(s => s.Provider)
                .SingleOrDefaultAsync(a => a.Id == id);
            if (appointment == null) return NotFound();
            if (appointment.Status is not (AppointmentStatus.Pending or AppointmentStatus.Approved))
            {
                TempData["Error"] = "Only pending or approved appointments can be cancelled.";
                return RedirectToAction(nameof(Appointments));
            }
            if (appointment.Slot.StartUtc <= _clock.GetUtcNow().UtcDateTime)
            {
                TempData["Error"] = "This appointment has already started, so it can't be cancelled.";
                return RedirectToAction(nameof(Appointments));
            }
            appointment.Status = AppointmentStatus.Cancelled;
            await Context.SaveChangesAsync();

            var when = $"{_time.ToLocal(appointment.Slot.StartUtc):ddd, d MMM yyyy HH:mm}";
            await _notificationService.SendAsync(appointment.ClientId,
                $"Your appointment with {appointment.Slot.Provider.Name} on {when} was cancelled by an administrator.", NotificationType.Appointment);
            if (appointment.Slot.Provider.PersonId is string providerPersonId)
            {
                await _notificationService.SendAsync(providerPersonId,
                    $"The appointment on {when} was cancelled by an administrator.", NotificationType.Appointment);
            }
            await AuditAsync("Cancelled", "Appointment", appointment.ClientId, $"Appointment {id}");
            TempData["Success"] = "Appointment cancelled. The client has been notified.";
            return RedirectToAction(nameof(Appointments));
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            ViewData["MailConfigured"] = HttpContext.RequestServices.GetRequiredService<IMailSender>().IsConfigured;
            return View(await SystemSettings.LoadAsync(Context));
        }

        [HttpPost]
        public async Task<IActionResult> Settings(SystemSettings model)
        {
            if (model.BannerEnabled && string.IsNullOrWhiteSpace(model.BannerMessage))
            {
                ModelState.AddModelError(nameof(model.BannerMessage), "Enter a message to show the banner.");
            }
            if (!ModelState.IsValid)
            {
                ViewData["MailConfigured"] = HttpContext.RequestServices.GetRequiredService<IMailSender>().IsConfigured;
                return View(model);
            }

            var settings = await Context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SystemSettings();
                Context.SystemSettings.Add(settings);
            }
            settings.BannerEnabled = model.BannerEnabled;
            settings.BannerMessage = string.IsNullOrWhiteSpace(model.BannerMessage) ? null : model.BannerMessage.Trim();
            settings.TemporaryPasswordHours = model.TemporaryPasswordHours;
            settings.MaxFailedAccessAttempts = model.MaxFailedAccessAttempts;
            settings.LockoutMinutes = model.LockoutMinutes;
            settings.ImportMaxRows = model.ImportMaxRows;
            settings.WeeklyReportEnabled = model.WeeklyReportEnabled;
            await Context.SaveChangesAsync();
            settings.ApplyLockout(UserManager.Options);

            await AuditAsync("Updated", "SystemSettings");
            TempData["Success"] = "Settings saved.";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        public async Task<IActionResult> SendWeeklyReport()
        {
            try
            {
                var count = await HttpContext.RequestServices.GetRequiredService<WeeklyReportSender>().SendAsync(HttpContext.RequestAborted);
                TempData["Success"] = $"Weekly report emailed to {count} {(count == 1 ? "admin" : "admins")}.";
            }
            catch (Exception ex) when (ex is InvalidOperationException or MailKit.Net.Smtp.SmtpCommandException or MailKit.ProtocolException or System.Net.Sockets.SocketException or IOException)
            {
                _logger.LogWarning(ex, "Could not send the weekly report");
                TempData["Error"] = ex is InvalidOperationException ? ex.Message : "The report could not be emailed. Check the mail server settings.";
            }
            return RedirectToAction(nameof(Settings));
        }

        [HttpGet]
        public async Task<IActionResult> ExportUserData(string id)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var data = new Dictionary<string, object?>
            {
                ["exportedAtUtc"] = _clock.GetUtcNow().UtcDateTime,
                ["profile"] = new { user.Id, user.Name, user.Email, Role = user.RoleDisplayName, user.DateOfBirth, user.CreatedAt, user.LastLoginAt, user.EmailNotificationsEnabled },
                ["notifications"] = await Context.Notifications.AsNoTracking().Where(n => n.PersonId == id).OrderBy(n => n.CreatedAt).ToListAsync(),
                ["loginHistory"] = await Context.LoginHistories.AsNoTracking().Where(l => l.PersonId == id).OrderBy(l => l.LoggedInAt).ToListAsync()
            };
            if (user is Client client)
            {
                data["clientProfile"] = new { client.HeightCm, client.FitnessGoal, client.TargetWeightKg, client.TargetDate };
                data["healthMetrics"] = await Context.HealthMetrics.AsNoTracking().Where(m => m.ClientId == id).OrderBy(m => m.RecordedAt).ToListAsync();
                data["workoutLogs"] = await Context.WorkoutLogs.AsNoTracking().Where(w => w.ClientId == id).OrderBy(w => w.LoggedAt).ToListAsync();
                data["mealLogs"] = await Context.MealLogs.AsNoTracking().Where(m => m.ClientId == id).OrderBy(m => m.LoggedAt).ToListAsync();
                data["appointments"] = await Context.Appointments.AsNoTracking().Where(a => a.ClientId == id).OrderBy(a => a.Slot.StartUtc)
                    .Select(a => new { a.Id, Provider = a.Slot.Provider.Name, a.Slot.StartUtc, a.Slot.EndUtc, Status = a.Status.ToString(), a.Notes, a.Rating, a.RatingComment, a.RequestedAtUtc })
                    .ToListAsync();
                data["workoutPlans"] = await Context.WorkoutPlans.AsNoTracking().Where(p => p.ClientId == id).Include(p => p.Items).ToListAsync();
                data["dietPlans"] = await Context.DietPlans.AsNoTracking().Where(p => p.ClientId == id).Include(p => p.Meals).ToListAsync();
                data["trainerFeedback"] = await Context.TrainerFeedbacks.AsNoTracking().Where(f => f.ClientId == id).ToListAsync();
                data["weeklyCheckIns"] = await Context.WeeklyCheckIns.AsNoTracking().Where(c => c.ClientId == id).ToListAsync();
                data["trainerAssignments"] = await Context.TrainerAssignmentHistories.AsNoTracking().Where(h => h.ClientId == id)
                    .Select(h => new { Trainer = h.Trainer!.Name, h.AssignedAtUtc, h.EndedAtUtc }).ToListAsync();
            }
            else if (user is Trainer)
            {
                data["assignedClients"] = await Context.ClientTrainerAssignments.AsNoTracking().Where(a => a.TrainerId == id)
                    .Select(a => new { Client = a.Client.Name, a.AssignedAtUtc }).ToListAsync();
                data["workoutPlans"] = await Context.WorkoutPlans.AsNoTracking().Where(p => p.TrainerId == id).Include(p => p.Items).ToListAsync();
                data["dietPlans"] = await Context.DietPlans.AsNoTracking().Where(p => p.TrainerId == id).Include(p => p.Meals).ToListAsync();
                data["feedbackGiven"] = await Context.TrainerFeedbacks.AsNoTracking().Where(f => f.TrainerId == id).ToListAsync();
            }

            var json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            _logger.LogInformation("Admin {AdminId} exported data for user {UserId}", CurrentUserId, id);
            await AuditAsync("Exported", "UserData", user is Client ? id : null, user.Name);
            return File(json, "application/json", $"healthtrack-data-{user.Id[..8]}-{DateTime.Today:yyyyMMdd}.json");
        }

        [HttpGet]
        public async Task<IActionResult> SystemHealth()
        {
            return View(await ReportData.GetSystemHealthAsync(MailConfigured));
        }

        private AdminReportService ReportData => new(Context, _time);

        private bool MailConfigured => HttpContext.RequestServices.GetRequiredService<IMailSender>().IsConfigured;

        [HttpGet]
        public IActionResult ImportUsers()
        {
            return View(new UserImportResult());
        }

        [HttpGet]
        public IActionResult ImportTemplate()
        {
            return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(UserImportService.Template)).ToArray(), "text/csv", "HealthTrack_UserImport_Template.csv");
        }

        [HttpPost]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ImportUsers(IFormFile? file)
        {
            var result = new UserImportResult();
            if (file == null || file.Length == 0)
            {
                result.FileError = "Choose a CSV file to import.";
            }
            else if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                result.FileError = "Only .csv files can be imported.";
            }
            else if (file.Length > UserImportService.MaxBytes)
            {
                result.FileError = "The file is larger than 1 MB.";
            }
            else
            {
                await using var stream = file.OpenReadStream();
                result = await _userImport.ImportAsync(stream);
                await AuditAsync("Imported", "Users", null, $"{result.Imported.Count} imported, {result.Rejected.Count} rejected");
                _logger.LogInformation("Admin {AdminId} imported {Imported} users ({Rejected} rejected)", CurrentUserId, result.Imported.Count, result.Rejected.Count);
            }
            return View(result);
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
                DateOfBirth = model.DateOfBirth!.Value,
                MustChangePassword = true,
                TemporaryPasswordExpiresAtUtc = await TemporaryPasswordExpiryAsync()
            };

            var result = await AccountProvisioner.CreateAsync(Context, UserManager, admin, model.Password, AppRoles.Admin);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            _logger.LogInformation("Admin {AdminId} created administrator {Email}", CurrentUserId, admin.Email);
            TempData["Success"] = $"Administrator {admin.Name} created. They must choose a new password at first sign-in.";
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
                DateOfBirth = model.DateOfBirth!.Value,
                MustChangePassword = true,
                TemporaryPasswordExpiresAtUtc = await TemporaryPasswordExpiryAsync()
            };

            var result = await AccountProvisioner.CreateAsync(Context, UserManager, trainer, model.Password, AppRoles.Trainer, model.Specialty);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            _logger.LogInformation("Admin {AdminId} created trainer {Email}", CurrentUserId, trainer.Email);
            TempData["Success"] = $"Trainer {trainer.Name} created. They must choose a new password at first sign-in.";
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
            var result = await SetLockAsync(user, !isLocked);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Users));
            }

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

            var error = await DeleteAccountAsync(user);
            if (error != null)
            {
                TempData["Error"] = error;
                return RedirectToAction(nameof(Users));
            }

            TempData["Success"] = $"{user.Name} has been deleted.";
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            ViewData["TargetRole"] = user.RoleDisplayName;
            var provider = user is Trainer ? await Context.ConsultationProviders.FirstOrDefaultAsync(p => p.PersonId == user.Id) : null;
            return View(new EditUserViewModel
            {
                Id = user.Id, Name = user.Name, Email = user.Email ?? string.Empty, DateOfBirth = user.DateOfBirth,
                IsTrainer = user is Trainer, Specialty = provider?.Specialty
            });
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            var user = await UserManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();
            ViewData["TargetRole"] = user.RoleDisplayName;
            model.IsTrainer = user is Trainer;
            ValidatePastDate(model.DateOfBirth, nameof(model.DateOfBirth));
            if (!ModelState.IsValid) return View(model);

            var email = model.Email.Trim();
            user.Name = model.Name.Trim();
            user.DateOfBirth = model.DateOfBirth!.Value;
            if (user is Trainer && await Context.ConsultationProviders.FirstOrDefaultAsync(p => p.PersonId == user.Id) is { } provider)
            {
                provider.Name = user.Name;
                provider.Specialty = string.IsNullOrWhiteSpace(model.Specialty) ? provider.Specialty : model.Specialty.Trim();
            }
            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = email;
                user.UserName = email;
                user.EmailConfirmed = true;
            }
            var result = await UserManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            _logger.LogInformation("Admin {AdminId} edited user {UserId}", CurrentUserId, user.Id);
            TempData["Success"] = $"{user.Name}'s details have been updated.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> SignOutUser(string id)
        {
            if (id == CurrentUserId)
            {
                TempData["Error"] = "Use Sign out to end your own session.";
                return RedirectToAction(nameof(Users));
            }
            var user = await UserManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            var result = await UserManager.UpdateSecurityStampAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Users));
            }
            _logger.LogInformation("Admin {AdminId} signed out user {UserId}", CurrentUserId, id);
            TempData["Success"] = $"{user.Name} has been signed out of all sessions.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> SendReminder(string id)
        {
            var client = await Context.Clients.SingleOrDefaultAsync(c => c.Id == id);
            if (client == null) return NotFound();
            await _notificationService.SendAsync(client.Id, ReminderMessage, NotificationType.General);
            TempData["Success"] = $"Reminder sent to {client.Name}.";
            return RedirectToAction(nameof(Users), new { status = "inactive" });
        }

        [HttpPost]
        public async Task<IActionResult> BulkUsers(string bulkAction, string[] ids, string? trainerId)
        {
            ids = ids.Distinct().ToArray();
            if (bulkAction == "export" && ids.Length > 0)
            {
                var rows = await QueryUserSummaries().Where(u => ids.Contains(u.Id)).OrderBy(u => u.Name).ToListAsync();
                var csv = _exportService.ToCsv(new[]
                {
                    new ReportSection("Users", new[] { "Name", "Email", "Role", "Joined", "Last active", "Status" },
                        rows.Select(u => new object?[] { u.Name, u.Email, u.Role, u.CreatedAt, u.LastLoginAt, u.IsLocked ? "Locked" : "Active" }))
                });
                return File(csv, "text/csv", $"healthtrack-users-{DateTime.Today:yyyy-MM-dd}.csv");
            }

            ids = ids.Where(i => i != CurrentUserId).ToArray();
            if (ids.Length == 0)
            {
                TempData["Error"] = "Select at least one user other than yourself.";
                return RedirectToAction(nameof(Users));
            }

            var users = await Context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            var done = 0;
            var skipped = new List<string>();
            string verb;
            switch (bulkAction)
            {
                case "lock":
                case "unlock":
                    verb = bulkAction == "lock" ? "locked" : "unlocked";
                    foreach (var user in users)
                    {
                        if ((await SetLockAsync(user, bulkAction == "lock")).Succeeded) done++; else skipped.Add(user.Name);
                    }
                    break;
                case "signout":
                    verb = "signed out";
                    foreach (var user in users)
                    {
                        if ((await UserManager.UpdateSecurityStampAsync(user)).Succeeded) done++; else skipped.Add(user.Name);
                    }
                    break;
                case "remind":
                    verb = "sent a reminder";
                    foreach (var user in users)
                    {
                        if (user is not Client) { skipped.Add(user.Name); continue; }
                        await _notificationService.SendAsync(user.Id, ReminderMessage, NotificationType.General);
                        done++;
                    }
                    break;
                case "assign":
                    var now = _clock.GetUtcNow();
                    var trainer = await Context.Trainers.SingleOrDefaultAsync(t => t.Id == trainerId && (t.LockoutEnd == null || t.LockoutEnd <= now));
                    if (trainer == null)
                    {
                        TempData["Error"] = "Choose a valid, unlocked Trainer to assign.";
                        return RedirectToAction(nameof(Users));
                    }
                    verb = $"assigned to {trainer.Name}";
                    var assigned = await AssignTrainerAsync(trainer, users.OfType<Client>().Select(c => c.Id).ToList(), now.UtcDateTime);
                    done = assigned;
                    skipped.AddRange(users.Where(u => u is not Client).Select(u => u.Name));
                    break;
                case "delete":
                    verb = "deleted";
                    foreach (var user in users)
                    {
                        if (await DeleteAccountAsync(user) == null) done++; else skipped.Add(user.Name);
                    }
                    break;
                default:
                    TempData["Error"] = "Choose a bulk action.";
                    return RedirectToAction(nameof(Users));
            }

            _logger.LogInformation("Admin {AdminId} bulk {Action} {Count} users", CurrentUserId, bulkAction, done);
            TempData["Success"] = $"{done} {(done == 1 ? "user" : "users")} {verb}.";
            if (skipped.Count > 0) TempData["Error"] = $"Skipped: {string.Join(", ", skipped)}.";
            return RedirectToAction(nameof(Users));
        }

        private async Task<DateTime> TemporaryPasswordExpiryAsync() =>
            _clock.GetUtcNow().UtcDateTime.AddHours((await SystemSettings.LoadAsync(Context)).TemporaryPasswordHours);

        private async Task<IdentityResult> SetLockAsync(Person user, bool lockAccount)
        {
            await using var operation = await AtomicOperation.BeginAsync(Context);
            var result = await UserManager.SetLockoutEnabledAsync(user, true);
            if (result.Succeeded) result = await UserManager.SetLockoutEndDateAsync(user, lockAccount ? DateTimeOffset.MaxValue : null);
            if (result.Succeeded) result = await UserManager.UpdateSecurityStampAsync(user);
            if (result.Succeeded) await operation.CommitAsync();
            return result;
        }

        private async Task<string?> DeleteAccountAsync(Person user)
        {
            var id = user.Id;
            if (user is Trainer && await Context.ClientTrainerAssignments.AnyAsync(a => a.TrainerId == id))
            {
                return "Reassign or remove this Trainer's Client assignments before deleting the account.";
            }
            await using var transaction = await AtomicOperation.BeginAsync(Context);
            if (user is Trainer)
            {
                var provider = await Context.ConsultationProviders.Include(p => p.Slots).FirstOrDefaultAsync(p => p.PersonId == id);
                if (provider != null)
                {
                    if (await Context.Appointments.AnyAsync(a => a.Slot.ProviderId == provider.Id))
                    {
                        return "This Trainer has linked sessions. Retain the account for existing consultations.";
                    }
                    Context.AvailabilitySlots.RemoveRange(provider.Slots);
                    Context.ConsultationProviders.Remove(provider);
                }
                Context.TrainerAssignmentHistories.RemoveRange(Context.TrainerAssignmentHistories.Where(h => h.TrainerId == id));
                await Context.SaveChangesAsync();
            }
            IdentityResult result;
            try { result = await UserManager.DeleteAsync(user); }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 })
            {
                Context.ChangeTracker.Clear();
                return "This account has linked records. Remove its current assignments or retain the account for existing consultations.";
            }
            if (!result.Succeeded)
            {
                return string.Join(" ", result.Errors.Select(e => e.Description));
            }
            await transaction.CommitAsync();

            _logger.LogInformation("Admin {AdminId} deleted user {UserId}", CurrentUserId, id);
            return null;
        }

        private async Task<int> AssignTrainerAsync(Trainer trainer, List<string> clientIds, DateTime now)
        {
            var clients = await Context.Clients.Include(c => c.TrainerAssignment)
                .Where(c => clientIds.Contains(c.Id)).ToListAsync();
            var changed = clients.Where(c => c.TrainerAssignment?.TrainerId != trainer.Id).ToList();
            var previous = new Dictionary<string, string?>();
            foreach (var client in changed)
            {
                previous[client.Id] = client.TrainerAssignment?.TrainerId;
                if (client.TrainerAssignment == null)
                {
                    Context.ClientTrainerAssignments.Add(new ClientTrainerAssignment { ClientId = client.Id, TrainerId = trainer.Id, AssignedAtUtc = now });
                }
                else
                {
                    client.TrainerAssignment.TrainerId = trainer.Id;
                    client.TrainerAssignment.AssignedAtUtc = now;
                }
                foreach (var row in await Context.TrainerAssignmentHistories.Where(h => h.ClientId == client.Id && h.EndedAtUtc == null).ToListAsync())
                {
                    row.EndedAtUtc = now;
                }
                Context.TrainerAssignmentHistories.Add(new TrainerAssignmentHistory { ClientId = client.Id, TrainerId = trainer.Id, AssignedAtUtc = now });
            }
            await Context.SaveChangesAsync();

            foreach (var client in changed)
            {
                await _notificationService.SendAsync(client.Id, $"{trainer.Name} is now your Trainer.", NotificationType.General);
                await _notificationService.SendAsync(trainer.Id, $"{client.Name} has been assigned to you.", NotificationType.General);
                if (previous[client.Id] is string oldTrainerId)
                    await _notificationService.SendAsync(oldTrainerId, $"{client.Name} is no longer assigned to you.", NotificationType.General);
            }
            return changed.Count;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ResetPassword(string id)
        {
            if (id == CurrentUserId) return BadRequest("Use Change password for your own account.");
            var user = await UserManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            if (user is Admin) return AdminResetBlocked();
            ViewData["TargetUserId"] = id;
            ViewData["TargetUserName"] = user.Name;
            return View(new ResetUserPasswordViewModel());
        }

        [HttpPost]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ResetPassword(string id, ResetUserPasswordViewModel model)
        {
            if (id == CurrentUserId) return BadRequest("Use Change password for your own account.");
            var user = await UserManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            if (user is Admin) return AdminResetBlocked();
            ViewData["TargetUserId"] = id;
            ViewData["TargetUserName"] = user.Name;
            if (!ModelState.IsValid) return View(model);
            await using var operation = await AtomicOperation.BeginAsync(Context);
            var token = await UserManager.GeneratePasswordResetTokenAsync(user);
            var result = await UserManager.ResetPasswordAsync(user, token, model.Password);
            if (!result.Succeeded) { AddErrors(result); return View(model); }
            var hours = (await SystemSettings.LoadAsync(Context)).TemporaryPasswordHours;
            user.MustChangePassword = true;
            user.TemporaryPasswordExpiresAtUtc = _clock.GetUtcNow().UtcDateTime.AddHours(hours);
            result = await UserManager.UpdateAsync(user);
            if (!result.Succeeded) { AddErrors(result); return View(model); }
            await operation.CommitAsync();
            TempData["Success"] = $"Temporary password reset for {user.Name}. Share it securely; it expires in {hours} hours and must be changed on sign-in.";
            return RedirectToAction(nameof(Users));
        }

        private IActionResult AdminResetBlocked()
        {
            TempData["Error"] = "Administrators reset their own password from Account. Another admin's password can't be reset here.";
            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> Reports(DateTime? from, DateTime? to)
        {
            if (from > to) (from, to) = (to, from);
            ViewData["From"] = from;
            ViewData["To"] = to;
            var reports = ReportData;
            ViewData["PopularActivities"] = await reports.GetPopularActivitiesAsync(10, from, to);
            var (planExercises, topPlans) = await reports.GetPlanRoutinesAsync(10, from, to);
            ViewData["PlanExercises"] = planExercises;
            ViewData["TopPlans"] = topPlans;
            return View(await reports.GetClientPerformanceAsync(from, to));
        }

        [HttpGet]
        public async Task<IActionResult> ExportReport(string format = "csv", string? report = null, DateTime? from = null, DateTime? to = null,
            string? actor = null, string? action = null)
        {
            if (from > to) (from, to) = (to, from);
            var sections = await ReportData.BuildSectionsAsync(report, from, to, MailConfigured, actor, action);
            var title = AdminReportService.Title(report);

            var fileName = $"HealthTrack_{title.Replace(' ', '_')}_{DateTime.Now:yyyyMMdd}";
            _logger.LogInformation("Admin {AdminId} exported {Report} as {Format}", CurrentUserId, title, format);
            await AuditAsync("Exported", title, null, format);
            if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                return File(_exportService.ToPdf("HealthTrack - " + title, sections), "application/pdf", fileName + ".pdf");
            }
            if (format.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return File(_exportService.ToXlsx(title, sections), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName + ".xlsx");
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
            var (entries, names) = await ReportData.GetAuditLogAsync(actor, action, from, to);
            ViewData["SubjectNames"] = names;
            return View(entries);
        }

        [HttpGet]
        public async Task<IActionResult> ChatQuestions()
        {
            var (questions, faqs) = await ReportData.GetChatQuestionsAsync();
            ViewData["Faqs"] = faqs;
            return View(questions);
        }

        [HttpPost]
        public async Task<IActionResult> SaveFaq(string question, string answer)
        {
            question = (question ?? string.Empty).Trim();
            answer = (answer ?? string.Empty).Trim();
            if (question.Length is 0 or > 300 || answer.Length is 0 or > 1000)
            {
                TempData["Error"] = "Enter a question (up to 300 characters) and an answer (up to 1000 characters).";
                return RedirectToAction(nameof(ChatQuestions));
            }
            var existing = await Context.ChatFaqs.FirstOrDefaultAsync(f => f.Question == question);
            if (existing != null) existing.Answer = answer;
            else Context.ChatFaqs.Add(new ChatFaq { Question = question, Answer = answer });
            await Context.SaveChangesAsync();
            await AuditAsync(existing != null ? "Updated" : "Created", "ChatFaq", null, question);
            TempData["Success"] = "Answer saved. The chatbot will use it for matching questions.";
            return RedirectToAction(nameof(ChatQuestions));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFaq(int id)
        {
            var faq = await Context.ChatFaqs.FindAsync(id);
            if (faq == null) return NotFound();
            Context.ChatFaqs.Remove(faq);
            await Context.SaveChangesAsync();
            await AuditAsync("Deleted", "ChatFaq", null, faq.Question);
            TempData["Success"] = "Saved answer removed.";
            return RedirectToAction(nameof(ChatQuestions));
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

            var sender = await UserManager.GetUserAsync(User);
            if (sender == null) return Challenge();
            await new AdminBroadcastService(Context, _time, _clock).SendAsync(sender.Id, sender.Name,
                model.RecipientId, model.Message, model.Type, model.ScheduledFor);

            TempData["Success"] = model.ScheduledFor.HasValue ? "Notification scheduled." : "Notification published. Email delivery is queued for eligible recipients.";
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

        private static async Task<Dictionary<DateTime, int>> CountPerDayAsync(IQueryable<DateTime> dates)
        {
            return await dates
                .GroupBy(d => d.Date)
                .Select(g => new DailyTotalViewModel { Date = g.Key, Total = g.Count() })
                .ToDictionaryAsync(d => d.Date, d => d.Total);
        }

        private const string AllTrainersRecipient = "all-trainers";

        private async Task<Dictionary<DateTime, int>> CountUtcPerDayAsync(IQueryable<DateTime> dates) =>
            (await dates.ToListAsync()).GroupBy(d => _time.ToLocal(d).Date).ToDictionary(g => g.Key, g => g.Count());

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
            var broadcasts = await Context.AdminBroadcasts.AsNoTracking().OrderByDescending(b => b.CreatedAtUtc).Take(50)
                .Select(b => new
                {
                    b.CreatedAtUtc, b.Type, b.Message, b.ScheduledFor, b.RecipientCount, b.RecipientDescription, b.SenderName,
                    Sent = b.Notifications.Count(n => n.EmailDelivery != null && n.EmailDelivery.SentAtUtc != null),
                    Pending = b.Notifications.Count(n => n.EmailDelivery != null && n.EmailDelivery.SentAtUtc == null
                        && n.EmailDelivery.CancelledAtUtc == null && n.EmailDelivery.Attempts < 5),
                    Failed = b.Notifications.Count(n => n.EmailDelivery != null && n.EmailDelivery.SentAtUtc == null
                        && n.EmailDelivery.CancelledAtUtc == null && n.EmailDelivery.Attempts >= 5)
                }).ToListAsync();
            var now = _time.ToLocal(_clock.GetUtcNow().UtcDateTime);
            ViewData["History"] = broadcasts.Select(b => new BroadcastHistoryItem(_time.ToLocal(b.CreatedAtUtc), b.Type,
                b.Message, b.ScheduledFor, b.RecipientCount, b.RecipientDescription, b.SenderName,
                b.ScheduledFor > now ? "Scheduled" : "Available in app", b.Sent, b.Pending, b.Failed)).ToList();
        }

        public IActionResult Providers() => RedirectToAction(nameof(Users), new { role = ProviderRole });

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

        [HttpPost]
        public async Task<IActionResult> DeleteProviderSlot(int id, int slotId)
        {
            var slot = await Context.AvailabilitySlots.Include(s => s.Appointments)
                .SingleOrDefaultAsync(s => s.Id == slotId && s.ProviderId == id && s.Provider.Kind == ConsultationProviderKind.HealthcareProfessional);
            if (slot == null) return NotFound();
            if (slot.Appointments.Any(a => a.Status != AppointmentStatus.Cancelled))
            {
                TempData["Error"] = "This slot has a booking. Cancel the appointment first.";
                return RedirectToAction(nameof(ProviderSlots), new { id });
            }
            if (slot.Appointments.Any()) slot.IsAvailable = false;
            else Context.AvailabilitySlots.Remove(slot);
            await Context.SaveChangesAsync();
            TempData["Success"] = "Availability slot removed.";
            return RedirectToAction(nameof(ProviderSlots), new { id });
        }

        private async Task<ProviderSlotsViewModel> LoadProviderSlotsAsync(ConsultationProvider provider, AddAvailabilitySlotForm form)
        {
            var nowUtc = _clock.GetUtcNow().UtcDateTime;
            var slots = await Context.AvailabilitySlots.AsNoTracking()
                .Where(s => s.ProviderId == provider.Id && s.IsAvailable && s.EndUtc > nowUtc)
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
