using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services
{
    public class NotificationService : INotificationService
    {
        private readonly HealthTrackDbContext _context;

        public NotificationService(HealthTrackDbContext context)
        {
            _context = context;
        }

        public async Task SendAsync(string personId, string message, NotificationType type, DateTime? scheduledFor = null)
        {
            _context.Notifications.Add(new Notification
            {
                PersonId = personId,
                Message = message,
                Type = type,
                ScheduledFor = scheduledFor
            });
            await _context.SaveChangesAsync();
        }

        public async Task SendToAllClientsAsync(string message, NotificationType type, DateTime? scheduledFor = null)
        {
            var clientIds = await _context.Clients.Select(c => c.Id).ToListAsync();
            _context.Notifications.AddRange(clientIds.Select(id => new Notification
            {
                PersonId = id,
                Message = message,
                Type = type,
                ScheduledFor = scheduledFor
            }));
            await _context.SaveChangesAsync();
        }

        public Task<List<Notification>> GetVisibleAsync(string personId, bool unreadOnly)
        {
            var now = DateTime.Now;
            return _context.Notifications
                .Where(n => n.PersonId == personId && (n.ScheduledFor == null || n.ScheduledFor <= now))
                .Where(n => !unreadOnly || !n.IsRead)
                .OrderByDescending(n => n.ScheduledFor ?? n.CreatedAt)
                .ToListAsync();
        }

        public Task<int> GetUnreadCountAsync(string personId)
        {
            var now = DateTime.Now;
            return _context.Notifications.CountAsync(n => n.PersonId == personId && !n.IsRead
                && (n.ScheduledFor == null || n.ScheduledFor <= now));
        }

        public async Task GenerateDailyRemindersAsync(string clientId)
        {
            var today = DateTime.Today;
            var remindedToday = await _context.Notifications
                .Where(n => n.PersonId == clientId && n.CreatedAt >= today)
                .Select(n => n.Type)
                .ToListAsync();

            if (!remindedToday.Contains(NotificationType.Workout)
                && !await _context.WorkoutLogs.AnyAsync(w => w.ClientId == clientId && w.LoggedAt >= today))
            {
                _context.Notifications.Add(new Notification
                {
                    PersonId = clientId,
                    Type = NotificationType.Workout,
                    Message = "You haven't logged a workout today. Keep your streak going!"
                });
            }

            if (!remindedToday.Contains(NotificationType.Meal)
                && !await _context.MealLogs.AnyAsync(m => m.ClientId == clientId && m.LoggedAt >= today))
            {
                _context.Notifications.Add(new Notification
                {
                    PersonId = clientId,
                    Type = NotificationType.Meal,
                    Message = "Don't forget to log your meals for today."
                });
            }

            var lastMetric = await _context.HealthMetrics
                .Where(m => m.ClientId == clientId)
                .MaxAsync(m => (DateTime?)m.RecordedAt);
            if (!remindedToday.Contains(NotificationType.General)
                && (lastMetric == null || lastMetric < today.AddDays(-7)))
            {
                _context.Notifications.Add(new Notification
                {
                    PersonId = clientId,
                    Type = NotificationType.General,
                    Message = "It's been over a week since your last weight check-in. Record a new measurement."
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}
