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

        // Controllers that work in the application's time zone override this.
        protected virtual DateTime LocalNow => DateTime.Now;

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
