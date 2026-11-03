using HealthTrack.Models;
using HealthTrack.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthTrack.ViewComponents
{
    public class UserMenuViewComponent : ViewComponent
    {
        private readonly UserManager<Person> _userManager;

        public UserMenuViewComponent(UserManager<Person> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var person = await _userManager.GetUserAsync(UserClaimsPrincipal);
            if (person == null)
            {
                return Content(string.Empty);
            }

            var model = new UserMenuViewModel
            {
                Name = person.Name,
                Email = person.Email ?? string.Empty,
                Initials = person.Initials,
                RoleDisplayName = person.RoleDisplayName
            };
            return View(model);
        }
    }
}
