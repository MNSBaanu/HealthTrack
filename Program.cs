using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using System.Threading.RateLimiting;
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<AuditSaveChangesInterceptor>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddDbContext<HealthTrackDbContext>((services, options) => options
    .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    .AddInterceptors(services.GetRequiredService<AuditSaveChangesInterceptor>()));
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
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
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
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddHttpClient<ChatAiClient>(client => client.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("chat", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddSingleton<IMailSender, MailSender>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<UserImportService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<AppointmentTime>();
builder.Services.AddScoped<AppointmentBookingService>();
builder.Services.AddScoped<TrainerAssignmentQueries>();
builder.Services.AddScoped<ITrainerPlanService, TrainerPlanService>();
builder.Services.AddScoped<ITrainerFeedbackService, TrainerFeedbackService>();
builder.Services.AddScoped<ITrainerScheduleService, TrainerScheduleService>();
builder.Services.AddHostedService<AppointmentReminderWorker>();
builder.Services.AddHostedService<DailyReminderWorker>();
builder.Services.AddHostedService<DataRetentionWorker>();
builder.Services.AddScoped<NotificationEmailDispatcher>();
builder.Services.AddHostedService<NotificationEmailWorker>();

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
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; "
        + "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; "
        + "font-src 'self' https://cdn.jsdelivr.net https://fonts.gstatic.com; img-src 'self' data:; "
        + "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
    headers["X-Frame-Options"] = "DENY";
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseMiddleware<TemporaryPasswordMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
