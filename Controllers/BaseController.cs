using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthTrack.Controllers
{
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

        protected void ValidateNotInFuture(DateTime date, string fieldName)
        {
            if (date > DateTime.Now)
            {
                ModelState.AddModelError(fieldName, "Date cannot be in the future.");
            }
        }

        protected void ValidatePastDate(DateTime? date, string fieldName)
        {
            if (date >= DateTime.Today)
            {
                ModelState.AddModelError(fieldName, "Date must be in the past.");
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
