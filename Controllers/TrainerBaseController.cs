using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace HealthTrack.Controllers
{
    [Authorize(Roles = AppRoles.Trainer)]
    public abstract class TrainerBaseController : BaseController
    {
        protected TrainerBaseController(HealthTrackDbContext context, UserManager<Person> userManager)
            : base(context, userManager)
        {
        }
    }
}
