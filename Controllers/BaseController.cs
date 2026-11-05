using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthTrack.Controllers
{
    // Shared base for app controllers: database context, current user and local time helpers.
    public abstract class BaseController : Controller
    {
        protected BaseController(HealthTrackDbContext context, UserManager<Person> userManager)
        {
            Context = context;
            UserManager = userManager;
        }

        protected HealthTrackDbContext Context { get; }

        protected UserManager<Person> UserManager { get; }

        protected string CurrentUserId => UserManager.GetUserId(User)!;

        protected Task AuditAsync(string action, string entity, string? subjectClientId = null, string? detail = null) =>
            HttpContext.RequestServices.GetRequiredService<HealthTrack.Services.IAuditService>()
                .RecordAsync(User, action, entity, subjectClientId, detail);

        // Current time in the application's time zone, the same clock used for reminders and scheduled notifications.
        protected virtual DateTime LocalNow => HttpContext.RequestServices.GetRequiredService<HealthTrack.Services.AppointmentTime>()
            .ToLocal(HttpContext.RequestServices.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime);

        protected void ValidateNotInFuture(DateTime date, string fieldName)
        {
            if (date > LocalNow)
            {
                ModelState.AddModelError(fieldName, "Date cannot be in the future.");
            }
        }

        protected void ValidatePastDate(DateTime? date, string fieldName)
        {
            if (date >= LocalNow.Date)
            {
                ModelState.AddModelError(fieldName, "Date must be in the past.");
            }
            else if (date < new DateTime(1900, 1, 1))
            {
                ModelState.AddModelError(fieldName, "Enter a valid date.");
            }
        }

        protected void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
