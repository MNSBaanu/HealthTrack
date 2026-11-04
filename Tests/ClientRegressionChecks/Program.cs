using System.Security.Claims;
using HealthTrack.Controllers;
using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using HealthTrack.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

var databaseName = "HealthTrack_ClientChecks_" + Guid.NewGuid().ToString("N");
var connection = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";
var clock = new MutableClock { Now = new DateTimeOffset(2026, 10, 29, 14, 0, 0, TimeSpan.Zero) };
var time = new AppointmentTime(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Appointments:TimeZoneId"] = "Asia/Colombo" }).Build());
var services = new ServiceCollection();
services.AddLogging(); services.AddMvcCore(); services.AddHttpContextAccessor();
services.AddDataProtection().UseEphemeralDataProtectionProvider();
services.AddAuthentication(IdentityConstants.ApplicationScheme).AddCookie(IdentityConstants.ApplicationScheme);
services.AddSingleton<AuditSaveChangesInterceptor>();
services.AddDbContext<HealthTrackDbContext>((sp, o) => o.UseSqlServer(connection).AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));
services.AddIdentityCore<Person>(o => o.Password.RequiredLength = 8).AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<HealthTrackDbContext>().AddSignInManager().AddDefaultTokenProviders();
services.AddScoped<IAuditService, AuditService>();
// Registered last so the fixed clock wins over any default TimeProvider.
services.AddSingleton<TimeProvider>(clock); services.AddSingleton(time);
using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope();
var sp = scope.ServiceProvider; var db = sp.GetRequiredService<HealthTrackDbContext>();
var users = sp.GetRequiredService<UserManager<Person>>(); var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
var mail = new FakeMail(); var notifications = new NotificationService(db, clock, time, mail);
var checks = 0; var ownsDatabase = false;
void Check(bool value, string name) { if (!value) throw new Exception("FAILED: " + name); checks++; }
DateTime LocalNow() => time.ToLocal(clock.Now.UtcDateTime);
DefaultHttpContext Http(string? id = null, string role = AppRoles.Client, string path = "/Client") {
    var http = new DefaultHttpContext { RequestServices = sp };
    http.Request.Scheme = "https"; http.Request.Host = new HostString("localhost"); http.Request.Path = path;
    if (id != null) http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
        new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Name, "Regression actor"), new Claim(ClaimTypes.Role, role)
    }, "checks"));
    sp.GetRequiredService<IHttpContextAccessor>().HttpContext = http; return http;
}
T Setup<T>(T controller, DefaultHttpContext http) where T : Controller {
    controller.ControllerContext = new ControllerContext { HttpContext = http, RouteData = new RouteData(), ActionDescriptor = new ControllerActionDescriptor() };
    controller.TempData = new TempDataDictionary(http, new MemoryTempData()); return controller;
}
var signIn = sp.GetRequiredService<SignInManager<Person>>();
AccountController Account(UserManager<Person> manager, DefaultHttpContext http) {
    signIn.Context = http;
    return Setup(new AccountController(NullLogger<AccountController>.Instance, db, manager, signIn, clock), http);
}
HealthMetricsController Metrics(string id) => Setup(new HealthMetricsController(NullLogger<HealthMetricsController>.Instance, db, users), Http(id, path: "/HealthMetrics"));
WorkoutLogsController Workouts(string id) => Setup(new WorkoutLogsController(NullLogger<WorkoutLogsController>.Instance, db, users), Http(id, path: "/WorkoutLogs"));
MealLogsController Meals(string id) => Setup(new MealLogsController(NullLogger<MealLogsController>.Instance, db, users), Http(id, path: "/MealLogs"));
ClientFeedbackController Feedback(string id) => Setup(new ClientFeedbackController(db, users, notifications), Http(id, path: "/ClientFeedback"));
ClientController ClientArea(string id) => Setup(new ClientController(NullLogger<ClientController>.Instance, db, users, notifications, null!, time, clock), Http(id));
async Task<Person> PersonAccount(Person person, string role) {
    person.UserName = person.Email; person.EmailConfirmed = true; person.DateOfBirth = new DateTime(1990, 1, 1);
    var result = await AccountProvisioner.CreateAsync(db, users, person, "TestOnly@12345", role);
    if (!result.Succeeded) throw new Exception(string.Join(" ", result.Errors.Select(e => e.Description)));
    return person;
}
async Task<WorkoutPlanItem> WorkoutItem(string clientId, string trainerId) {
    var plan = new WorkoutPlan { ClientId = clientId, TrainerId = trainerId, Title = "Regression plan", StartDate = LocalNow().Date.AddDays(-1),
        Items = { new WorkoutPlanItem { ExerciseName = "Regression squats", DurationMinutes = 20 } } };
    db.WorkoutPlans.Add(plan); await db.SaveChangesAsync(); return plan.Items.Single();
}
RegisterViewModel Registration(string email) => new() { Name = "Registered client", Email = email, DateOfBirth = new DateTime(1992, 2, 2),
    HeightCm = 172, WeightKg = 70, Password = "TestOnly@12345", ConfirmPassword = "TestOnly@12345", AcceptPrivacy = true };
try {
    if (await db.Database.CanConnectAsync()) throw new Exception("Refusing to reuse an existing database.");
    ownsDatabase = true; await db.Database.EnsureCreatedAsync();
    Http(); foreach (var role in new[] { AppRoles.Admin, AppRoles.Client, AppRoles.Trainer }) await roles.CreateAsync(new IdentityRole(role));
    var trainer = (Trainer)await PersonAccount(new Trainer { Name = "Regression trainer", Email = "trainer@example.invalid" }, AppRoles.Trainer);
    var client = (Client)await PersonAccount(new Client { Name = "Regression client", Email = "client@example.invalid", HeightCm = 170 }, AppRoles.Client);

    // Registration
    var registered = await Account(users, Http(path: "/Account/Register")).Register(Registration("  Registered@example.invalid "));
    var created = await users.FindByEmailAsync("registered@example.invalid");
    Check(registered is RedirectToActionResult && created != null, "registration succeeds and trims the email");
    Check(await users.IsInRoleAsync(created!, AppRoles.Client), "registered client has the Client role");
    Check(await db.HealthMetrics.AnyAsync(m => m.ClientId == created!.Id && m.WeightKg == 70), "registration stores the starting weight");
    var rejected = Account(new RejectRoleManager(sp), Http(path: "/Account/Register"));
    Check(await rejected.Register(Registration("orphan@example.invalid")) is ViewResult && !rejected.ModelState.IsValid, "role failure returns validation errors");
    Check(!await db.Users.AnyAsync(p => p.Email == "orphan@example.invalid"), "role failure leaves no role-less account");
    var tooOld = Account(users, Http(path: "/Account/Register")); var ancient = Registration("ancient@example.invalid"); ancient.DateOfBirth = new DateTime(1850, 1, 1);
    Check(await tooOld.Register(ancient) is ViewResult && !tooOld.ModelState.IsValid, "impossible birth dates are rejected");

    // Health metrics use the application's time zone, not the server's
    var metrics = Metrics(client.Id);
    Check(await metrics.Create(new HealthMetric { WeightKg = 70, RecordedAt = LocalNow() }) is RedirectToActionResult, "a measurement made now is accepted");
    metrics = Metrics(client.Id);
    Check(await metrics.Create(new HealthMetric { WeightKg = 70, RecordedAt = LocalNow().AddHours(1) }) is ViewResult && !metrics.ModelState.IsValid, "a future measurement is rejected");
    metrics = Metrics(client.Id);
    Check(await metrics.Create(new HealthMetric { WeightKg = 70, RecordedAt = LocalNow(), SystolicBp = 80, DiastolicBp = 120 }) is ViewResult && !metrics.ModelState.IsValid, "systolic must exceed diastolic");
    metrics = Metrics(client.Id);
    Check(await metrics.Create(new HealthMetric { WeightKg = 70, RecordedAt = LocalNow(), SystolicBp = 120 }) is ViewResult && !metrics.ModelState.IsValid, "blood pressure needs both values");
    metrics = Metrics(client.Id);
    Check(await metrics.Create(new HealthMetric { WeightKg = 70, RecordedAt = LocalNow(), SystolicBp = 120, DiastolicBp = 80 }) is RedirectToActionResult, "valid blood pressure is accepted");

    // Planned workouts and meals cannot be logged twice on the same day
    var item = await WorkoutItem(client.Id, trainer.Id);
    var planned = new WorkoutLog { Activity = "Regression squats", DurationMinutes = 20, LoggedAt = LocalNow(), WorkoutPlanItemId = item.Id };
    Check(await Workouts(client.Id).Create(planned) is RedirectToActionResult, "planned workout is logged");
    var duplicate = Workouts(client.Id);
    Check(await duplicate.Create(new WorkoutLog { Activity = "Regression squats", DurationMinutes = 20, LoggedAt = LocalNow(), WorkoutPlanItemId = item.Id }) is ViewResult && !duplicate.ModelState.IsValid, "duplicate planned workout is rejected");
    Check(await Workouts(client.Id).Create(item.Id) is RedirectToActionResult, "opening an already logged planned workout redirects");
    Check(await db.WorkoutLogs.CountAsync(w => w.ClientId == client.Id && w.WorkoutPlanItemId == item.Id) == 1, "only one planned workout entry exists");

    var dietPlan = new DietPlan { ClientId = client.Id, TrainerId = trainer.Id, Title = "Regression diet", DailyCalorieTarget = 2000, StartDate = LocalNow().Date.AddDays(-1),
        Meals = { new DietPlanMeal { MealType = MealType.Lunch, FoodRecommendations = "Rice and vegetables", ApproximateCalories = 600 } } };
    db.DietPlans.Add(dietPlan); await db.SaveChangesAsync(); var planMeal = dietPlan.Meals.Single();
    Check(await Meals(client.Id).Create(new MealLog { MealType = MealType.Lunch, Calories = 600, FoodItems = "  Rice  ", LoggedAt = LocalNow(), DietPlanMealId = planMeal.Id }) is RedirectToActionResult, "planned meal is logged");
    var duplicateMeal = Meals(client.Id);
    Check(await duplicateMeal.Create(new MealLog { MealType = MealType.Lunch, Calories = 600, LoggedAt = LocalNow(), DietPlanMealId = planMeal.Id }) is ViewResult && !duplicateMeal.ModelState.IsValid, "duplicate planned meal is rejected");
    Check((await db.MealLogs.SingleAsync(m => m.ClientId == client.Id)).FoodItems == "Rice", "food items are trimmed");

    // The dashboard counts calories for the application's local day
    var dashboard = (ClientDashboardViewModel)((ViewResult)await ClientArea(client.Id).Index()).Model!;
    Check(dashboard.CaloriesConsumedToday == 600 && dashboard.PlanToday == LocalNow().Date, "dashboard totals today's calories on the local date");

    // Daily reminders
    var reminderClient = (Client)await PersonAccount(new Client { Name = "Reminder client", Email = "reminder@example.invalid", HeightCm = 170 }, AppRoles.Client);
    db.Notifications.Add(new Notification { PersonId = reminderClient.Id, Type = NotificationType.Workout, Message = "A trainer note about workouts" });
    await db.SaveChangesAsync();
    await notifications.GenerateDailyRemindersAsync(reminderClient.Id); await notifications.GenerateDailyRemindersAsync(reminderClient.Id);
    Check(await db.Notifications.CountAsync(n => n.PersonId == reminderClient.Id && n.Message.StartsWith("You haven't logged a workout")) == 1,
        "a trainer's message does not suppress the workout reminder, and it is sent once");
    var doneClient = (Client)await PersonAccount(new Client { Name = "Checked-off client", Email = "checked@example.invalid", HeightCm = 170 }, AppRoles.Client);
    var doneItem = await WorkoutItem(doneClient.Id, trainer.Id);
    db.WorkoutPlanCompletions.Add(new WorkoutPlanCompletion { WorkoutPlanItemId = doneItem.Id, ClientId = doneClient.Id, CompletedOn = LocalNow().Date });
    await db.SaveChangesAsync(); await notifications.GenerateDailyRemindersAsync(doneClient.Id);
    Check(!await db.Notifications.AnyAsync(n => n.PersonId == doneClient.Id && n.Message.StartsWith("You haven't logged a workout")), "a checked-off planned exercise suppresses the workout reminder");
    Check(await db.Notifications.AnyAsync(n => n.PersonId == doneClient.Id && n.Message.StartsWith("Don't forget to log your meals")), "reminders use the local hour (19:30)");

    // Weekly check-ins and feedback replies are single-answer
    var checkIn = new WeeklyCheckIn { ClientId = client.Id, TrainerId = trainer.Id }; db.WeeklyCheckIns.Add(checkIn);
    var feedback = new TrainerFeedback { ClientId = client.Id, TrainerId = trainer.Id, Title = "Regression feedback", Comments = "Keep going" }; db.TrainerFeedbacks.Add(feedback);
    await db.SaveChangesAsync();
    await Feedback(client.Id).CheckIn(checkIn.Id, 4, 4, 1, "fine");
    var again = Feedback(client.Id); await again.CheckIn(checkIn.Id, 1, 1, 9, "changed");
    var saved = await db.WeeklyCheckIns.AsNoTracking().SingleAsync(c => c.Id == checkIn.Id);
    Check(saved.Energy == 4 && saved.Pain == 1 && again.TempData["Error"] != null, "a check-in is answered only once");
    Check(await db.Notifications.CountAsync(n => n.PersonId == trainer.Id && n.Message.Contains("weekly check-in")) == 1, "the trainer is notified once per check-in");
    await Feedback(client.Id).Reply(feedback.Id, "Thank you");
    var secondReply = Feedback(client.Id); await secondReply.Reply(feedback.Id, "Overwritten");
    Check((await db.TrainerFeedbacks.AsNoTracking().SingleAsync(f => f.Id == feedback.Id)).ClientReply == "Thank you" && secondReply.TempData["Error"] != null, "the first feedback reply is kept");
    Check(await db.Notifications.CountAsync(n => n.PersonId == trainer.Id && n.Message.Contains("replied to your feedback")) == 1, "the trainer is notified once per reply");

    // Account deletion removes the client and everything they own
    db.ChangeTracker.Clear(); // Mirror a fresh request: nothing is tracked when the client deletes their account.
    var deletion = ClientArea(client.Id);
    Check(await deletion.DeleteAccount(new DeleteAccountViewModel { Password = "wrong" }) is ViewResult && await db.Users.AnyAsync(u => u.Id == client.Id), "wrong password keeps the account");
    deletion = ClientArea(client.Id);
    Check(await deletion.DeleteAccount(new DeleteAccountViewModel { Password = "TestOnly@12345" }) is RedirectToActionResult, "correct password deletes the account");
    Check(!await db.Users.AnyAsync(u => u.Id == client.Id) && !await db.HealthMetrics.AnyAsync(m => m.ClientId == client.Id)
        && !await db.WorkoutLogs.AnyAsync(w => w.ClientId == client.Id) && !await db.WeeklyCheckIns.AnyAsync(c => c.ClientId == client.Id), "deleted client leaves no data behind");
    Console.WriteLine($"Passed {checks} Client regression checks. Email sender was fake; application data was not used.");
}
finally {
    if (ownsDatabase && databaseName.StartsWith("HealthTrack_ClientChecks_", StringComparison.Ordinal)) {
        sp.GetRequiredService<IHttpContextAccessor>().HttpContext = null;
        await db.Database.EnsureDeletedAsync(); Console.WriteLine("Isolated regression database removed.");
    }
}

class MutableClock : TimeProvider { public DateTimeOffset Now { get; set; } public override DateTimeOffset GetUtcNow() => Now; }
class FakeMail : IMailSender {
    public bool Configured { get; set; } public bool IsConfigured => Configured;
    public List<string> Messages { get; } = new();
    public void QueueNotification(string address, string name, string message) => Messages.Add(message);
    public Task SendNotificationAsync(string address, string name, string message, CancellationToken token = default) { Messages.Add(message); return Task.CompletedTask; }
}
class MemoryTempData : ITempDataProvider { public IDictionary<string, object> LoadTempData(HttpContext c) => new Dictionary<string, object>(); public void SaveTempData(HttpContext c, IDictionary<string, object> values) { } }
class RejectRoleManager(IServiceProvider p) : UserManager<Person>(p.GetRequiredService<IUserStore<Person>>(), p.GetRequiredService<IOptions<IdentityOptions>>(), p.GetRequiredService<IPasswordHasher<Person>>(), p.GetServices<IUserValidator<Person>>(), p.GetServices<IPasswordValidator<Person>>(), p.GetRequiredService<ILookupNormalizer>(), p.GetRequiredService<IdentityErrorDescriber>(), p, p.GetRequiredService<ILogger<UserManager<Person>>>()) {
    public override Task<IdentityResult> AddToRoleAsync(Person user, string role) => Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Injected role assignment failure" }));
}
