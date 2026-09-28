using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace HealthTrack.Controllers
{
    [Authorize(Roles = AppRoles.Client)]
    public abstract class ClientBaseController : BaseController
    {
        protected ClientBaseController(HealthTrackDbContext context, UserManager<Person> userManager)
            : base(context, userManager)
        {
        }
    }
}
