using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    [Authorize]
    public class NotificationsController : BaseController
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(HealthTrackDbContext context, UserManager<Person> userManager, INotificationService notificationService)
            : base(context, userManager)
        {
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _notificationService.GetVisibleAsync(CurrentUserId, unreadOnly: false));
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            return Json(await _notificationService.GetUnreadCountAsync(CurrentUserId));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await Context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.PersonId == CurrentUserId);
            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;
            await Context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await Context.Notifications
                .Where(n => n.PersonId == CurrentUserId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await Context.Notifications
                .Where(n => n.NotificationId == id && n.PersonId == CurrentUserId)
                .ExecuteDeleteAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
