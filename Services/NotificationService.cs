using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace HealthTrack.Services
{
    public class NotificationService : INotificationService
    {
        private readonly HealthTrackDbContext _context;
        private readonly TimeProvider _clock;
        private readonly AppointmentTime _appointmentTime;

        public NotificationService(HealthTrackDbContext context, TimeProvider clock, AppointmentTime appointmentTime)
        {
            _context = context;
            _clock = clock;
            _appointmentTime = appointmentTime;
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

        public async Task<List<Notification>> GetVisibleAsync(string personId, bool unreadOnly)
        {
            await GenerateAppointmentRemindersAsync(personId);
            var now = DateTime.Now;
            return await _context.Notifications
                .Where(n => n.PersonId == personId && !n.IsDismissed && (n.ScheduledFor == null || n.ScheduledFor <= now))
                .Where(n => !unreadOnly || !n.IsRead)
                .OrderByDescending(n => n.ScheduledFor ?? n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string personId)
        {
            await GenerateAppointmentRemindersAsync(personId);
            var now = DateTime.Now;
            return await _context.Notifications.CountAsync(n => n.PersonId == personId && !n.IsRead && !n.IsDismissed
                && (n.ScheduledFor == null || n.ScheduledFor <= now));
        }

        public async Task GenerateDailyRemindersAsync(string clientId)
        {
            var client = await _context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clientId);
            if (client == null) return;
            var today = DateTime.Today;
            var hour = _clock.GetLocalNow().Hour;
            var remindedToday = await _context.Notifications
                .Where(n => n.PersonId == clientId && n.CreatedAt >= today)
                .Select(n => n.Type)
                .ToListAsync();

            if (client.WorkoutReminderEnabled && hour >= client.WorkoutReminderHour
                && !remindedToday.Contains(NotificationType.Workout)
                && !await _context.WorkoutLogs.AnyAsync(w => w.ClientId == clientId && w.LoggedAt >= today))
            {
                _context.Notifications.Add(new Notification
                {
                    PersonId = clientId,
                    Type = NotificationType.Workout,
                    Message = "You haven't logged a workout today. Keep your streak going!"
                });
            }

            if (client.MealReminderEnabled && hour >= client.MealReminderHour
                && !remindedToday.Contains(NotificationType.Meal)
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
            if (client.WeightReminderEnabled && hour >= client.WeightReminderHour
                && !remindedToday.Contains(NotificationType.General)
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

        public async Task GenerateAppointmentRemindersAsync(string clientId)
        {
            // A page request and the background worker may discover the same reminder.
            // The unique appointment index prevents duplicates; retry against saved state.
            for (var attempt = 0; ; attempt++)
            {
                try { await SynchronizeAppointmentRemindersAsync(clientId); return; }
                catch (DbUpdateException exception) when (attempt == 0
                    && exception.InnerException is SqlException { Number: 2601 or 2627 })
                {
                    foreach (var entry in _context.ChangeTracker.Entries<Notification>().ToList())
                        entry.State = EntityState.Detached;
                }
            }
        }

        private async Task SynchronizeAppointmentRemindersAsync(string clientId)
        {
            var now = _clock.GetUtcNow().UtcDateTime;
            var cutoff = now.AddHours(24);
            var upcoming = await _context.Appointments.AsNoTracking()
                .Include(a => a.Slot).ThenInclude(s => s.Provider)
                .Where(a => a.ClientId == clientId && a.Status == AppointmentStatus.Approved
                    && a.Slot.StartUtc > now && a.Slot.StartUtc <= cutoff)
                .ToListAsync();
            var reminders = await _context.Notifications
                .Where(n => n.PersonId == clientId && n.AppointmentId != null).ToListAsync();
            var upcomingIds = upcoming.Select(a => a.Id).ToHashSet();
            _context.Notifications.RemoveRange(reminders.Where(n => !upcomingIds.Contains(n.AppointmentId!.Value)));
            foreach (var appointment in upcoming)
            {
                var start = _appointmentTime.ToLocal(appointment.Slot.StartUtc);
                var message = $"Appointment reminder: consultation with {appointment.Slot.Provider.Name} on {start:ddd, d MMM yyyy HH:mm} ({_appointmentTime.Label}).";
                if (message.Length > 300) message = message[..300];
                var reminder = reminders.SingleOrDefault(n => n.AppointmentId == appointment.Id);
                if (reminder == null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        PersonId = clientId, Type = NotificationType.Appointment,
                        AppointmentId = appointment.Id, AppointmentStartUtc = appointment.Slot.StartUtc,
                        Message = message
                    });
                }
                else if (reminder.AppointmentStartUtc != appointment.Slot.StartUtc || reminder.Message != message)
                {
                    reminder.AppointmentStartUtc = appointment.Slot.StartUtc;
                    reminder.Message = message;
                    reminder.IsRead = false;
                    reminder.IsDismissed = false;
                    reminder.CreatedAt = DateTime.Now;
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}
