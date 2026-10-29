using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using HealthTrack.Models.ViewModels;

namespace HealthTrack.Controllers
{
    [Authorize(Roles = AppRoles.Client)]
    public abstract class ClientBaseController : BaseController
    {
        protected ClientBaseController(HealthTrackDbContext context, UserManager<Person> userManager)
            : base(context, userManager)
        {
        }

        protected bool SetJournalFilters(DateTime? from, DateTime? to, string? q = null, MealType? mealType = null, bool meals = false)
        {
            ViewData["JournalFilter"] = new JournalFilterViewModel
                { From = from, To = to, Q = q, MealType = mealType, IsMealJournal = meals };
            if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
                ModelState.AddModelError(string.Empty, "The start date must be on or before the end date.");
            if (mealType.HasValue && !Enum.IsDefined(mealType.Value))
                ModelState.AddModelError(nameof(mealType), "Choose a valid meal type.");
            return ModelState.IsValid;
        }
    }
}
