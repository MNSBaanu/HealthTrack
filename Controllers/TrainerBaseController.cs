using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace HealthTrack.Controllers
{
    // Base for trainer-only pages; restricts access to the Trainer role.
    [Authorize(Roles = AppRoles.Trainer)]
    public abstract class TrainerBaseController : BaseController
    {
        protected TrainerBaseController(HealthTrackDbContext context, UserManager<Person> userManager)
            : base(context, userManager)
        {
        }
    }
}
