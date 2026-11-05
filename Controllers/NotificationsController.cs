using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    // Notification centre for reminders and alerts.
    [Authorize]
    public class NotificationsController : BaseController
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(HealthTrackDbContext context, UserManager<Person> userManager, INotificationService notificationService)
            : base(context, userManager)
        {
            _notificationService = notificationService;
        }

        // Lists the user's notifications.
        public async Task<IActionResult> Index()
        {
            return View(await _notificationService.GetVisibleAsync(CurrentUserId, unreadOnly: false));
        }

        // Returns the unread count as JSON for the notification bell.
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            return Json(await _notificationService.GetUnreadCountAsync(CurrentUserId));
        }

        // Marks one notification as read.
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await Context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.PersonId == CurrentUserId && !n.IsDismissed
                    && (n.ScheduledFor == null || n.ScheduledFor <= DateTime.Now));
            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;
            await Context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Marks all of the user's notifications as read.
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notificationService.GenerateAppointmentRemindersAsync(CurrentUserId);
            var now = DateTime.Now;
            await Context.Notifications
                .Where(n => n.PersonId == CurrentUserId && !n.IsRead && !n.IsDismissed
                    && (n.ScheduledFor == null || n.ScheduledFor <= now))
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
            return RedirectToAction(nameof(Index));
        }

        // Dismisses a notification.
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            // Keep a dismissed automatic reminder so refreshes do not recreate it.
            await Context.Notifications
                .Where(n => n.NotificationId == id && n.PersonId == CurrentUserId && n.AppointmentId != null)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsDismissed, true));
            await Context.Notifications
                .Where(n => n.NotificationId == id && n.PersonId == CurrentUserId && n.AppointmentId == null)
                .ExecuteDeleteAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
