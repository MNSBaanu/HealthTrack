using HealthTrack.Models;
using HealthTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthTrack.ViewComponents
{
    // Renders the header notification bell with the unread count.
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<Person> _userManager;

        public NotificationBellViewComponent(INotificationService notificationService, UserManager<Person> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        // Loads the unread count for the current user.
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var personId = _userManager.GetUserId(UserClaimsPrincipal);
            var unreadCount = personId == null ? 0 : await _notificationService.GetUnreadCountAsync(personId);
            return View(unreadCount);
        }
    }
}
