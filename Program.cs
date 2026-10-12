using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddDbContext<HealthTrackDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<Person, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddEntityFrameworkStores<HealthTrackDbContext>()
    .AddDefaultTokenProviders();
// Check persisted account state on every request, including existing sessions.
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnValidatePrincipal = async cookieContext =>
    {
        await SecurityStampValidator.ValidatePrincipalAsync(cookieContext);
        if (cookieContext.Principal?.Identity?.IsAuthenticated != true) return;
        var users = cookieContext.HttpContext.RequestServices.GetRequiredService<UserManager<Person>>();
        var user = await users.GetUserAsync(cookieContext.Principal);
        if (user == null || await users.IsLockedOutAsync(user))
        {
            cookieContext.RejectPrincipal();
            await cookieContext.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    };
});
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<AppointmentTime>();
builder.Services.AddScoped<AppointmentBookingService>();
builder.Services.AddScoped<TrainerAssignmentQueries>();
builder.Services.AddScoped<ITrainerPlanService, TrainerPlanService>();
builder.Services.AddScoped<ITrainerFeedbackService, TrainerFeedbackService>();
builder.Services.AddScoped<ITrainerScheduleService, TrainerScheduleService>();
builder.Services.AddHostedService<AppointmentReminderWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<HealthTrackDbContext>().Database.Migrate();
    await DbSeeder.SeedAsync(scope.ServiceProvider, builder.Configuration);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
