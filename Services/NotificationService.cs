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
        private readonly IMailSender _mail;

        public NotificationService(HealthTrackDbContext context, TimeProvider clock, AppointmentTime appointmentTime, IMailSender mail)
        {
            _context = context;
            _clock = clock;
            _appointmentTime = appointmentTime;
            _mail = mail;
        }

        // Scheduled notifications are only shown later, so they are not emailed now.
        private async Task EmailAsync(IReadOnlyCollection<string> personIds, string message, DateTime? scheduledFor = null)
        {
            if (!_mail.IsConfigured || (scheduledFor != null && scheduledFor > DateTime.Now))
            {
                return;
            }

            var recipients = await _context.Users.AsNoTracking()
                .Where(u => personIds.Contains(u.Id) && u.EmailNotificationsEnabled && u.Email != null)
                .Select(u => new { u.Email, u.Name })
                .ToListAsync();
            foreach (var recipient in recipients)
            {
                _mail.QueueNotification(recipient.Email!, recipient.Name, message);
            }
        }

        public async Task SendAsync(string personId, string message, NotificationType type, DateTime? scheduledFor = null)
        {
            var notification = new Notification
            {
                PersonId = personId,
                Message = message.Length > 300 ? message[..300] : message,
                Type = type,
                ScheduledFor = scheduledFor
            };
            AddScheduledEmail(notification, scheduledFor);
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            if (!scheduledFor.HasValue) await EmailAsync(new[] { personId }, message);
        }

        public async Task SendToAllClientsAsync(string message, NotificationType type, DateTime? scheduledFor = null)
        {
            var clientIds = await _context.Clients.Select(c => c.Id).ToListAsync();
            var notifications = clientIds.Select(id => new Notification
            {
                PersonId = id,
                Message = message,
                Type = type,
                ScheduledFor = scheduledFor
            }).ToList();
            foreach (var notification in notifications) AddScheduledEmail(notification, scheduledFor);
            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
            if (!scheduledFor.HasValue) await EmailAsync(clientIds, message);
        }

        public async Task SendToAllTrainersAsync(string message, NotificationType type, DateTime? scheduledFor = null)
        {
            var trainerIds = await _context.Trainers.Select(t => t.Id).ToListAsync();
            var notifications = trainerIds.Select(id => new Notification
            {
                PersonId = id,
                Message = message,
                Type = type,
                ScheduledFor = scheduledFor
            }).ToList();
            foreach (var notification in notifications) AddScheduledEmail(notification, scheduledFor);
            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
            if (!scheduledFor.HasValue) await EmailAsync(trainerIds, message);
        }

        private void AddScheduledEmail(Notification notification, DateTime? scheduledFor)
        {
            if (scheduledFor.HasValue)
                notification.EmailDelivery = new NotificationEmail { DueAtUtc = _appointmentTime.ToUtc(scheduledFor.Value) };
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
            // Reminders follow the application's time zone, like the client's journals and plans.
            var now = _appointmentTime.ToLocal(_clock.GetUtcNow().UtcDateTime);
            var today = now.Date;
            var hour = now.Hour;
            // Match on the message so unrelated notifications of the same type (e.g. a
            // trainer's message) never suppress a reminder.
            var remindedToday = await _context.Notifications
                .Where(n => n.PersonId == clientId && n.CreatedAt >= today)
                .Select(n => n.Message)
                .ToListAsync();
            var reminders = new List<string>();

            void AddReminder(NotificationType type, string message)
            {
                _context.Notifications.Add(new Notification
                {
                    PersonId = clientId, Type = type, Message = message, CreatedAt = now
                });
                reminders.Add(message);
            }

            if (client.WorkoutReminderEnabled && hour >= client.WorkoutReminderHour
                && !remindedToday.Contains(WorkoutReminderMessage)
                && !await _context.WorkoutLogs.AnyAsync(w => w.ClientId == clientId && w.LoggedAt >= today)
                // Ticking off a planned exercise also counts as training today.
                && !await _context.WorkoutPlanCompletions.AnyAsync(c => c.ClientId == clientId && c.CompletedOn == today))
                AddReminder(NotificationType.Workout, WorkoutReminderMessage);

            if (client.MealReminderEnabled && hour >= client.MealReminderHour
                && !remindedToday.Contains(MealReminderMessage)
                && !await _context.MealLogs.AnyAsync(m => m.ClientId == clientId && m.LoggedAt >= today))
                AddReminder(NotificationType.Meal, MealReminderMessage);

            var lastMetric = await _context.HealthMetrics
                .Where(m => m.ClientId == clientId)
                .MaxAsync(m => (DateTime?)m.RecordedAt);
            if (client.WeightReminderEnabled && hour >= client.WeightReminderHour
                && !remindedToday.Contains(WeightReminderMessage)
                && (lastMetric == null || lastMetric < today.AddDays(-7)))
                AddReminder(NotificationType.General, WeightReminderMessage);

            if (reminders.Count == 0) return;
            await _context.SaveChangesAsync();
            foreach (var reminder in reminders)
            {
                await EmailAsync(new[] { clientId }, reminder);
            }
        }

        private const string WorkoutReminderMessage = "You haven't logged a workout today. Keep your streak going!";
        private const string MealReminderMessage = "Don't forget to log your meals for today.";
        private const string WeightReminderMessage = "It's been over a week since your last weight check-in. Record a new measurement.";

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
