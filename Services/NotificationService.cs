using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace HealthTrack.Services
{
    // Creates and reads in-app notifications and generates reminders.
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

        // Scheduled times are entered in the application's time zone, so they are compared with the same clock.
        private DateTime LocalNow => _appointmentTime.ToLocal(_clock.GetUtcNow().UtcDateTime);

        // Scheduled notifications are only shown later, so they are not emailed now.
        private async Task EmailAsync(IReadOnlyCollection<string> personIds, string message, DateTime? scheduledFor = null)
        {
            if (!_mail.IsConfigured || (scheduledFor != null && scheduledFor > LocalNow))
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

        // Creates a notification for one user.
        public async Task SendAsync(string personId, string message, NotificationType type, DateTime? scheduledFor = null)
        {
            var notification = new Notification
            {
                PersonId = personId,
                Message = message.Length > 300 ? message[..300] : message,
                Type = type,
                ScheduledFor = scheduledFor,
                CreatedAt = LocalNow
            };
            AddScheduledEmail(notification, scheduledFor);
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            if (!scheduledFor.HasValue) await EmailAsync(new[] { personId }, message);
        }

        // Creates a notification for every client.
        public async Task SendToAllClientsAsync(string message, NotificationType type, DateTime? scheduledFor = null)
        {
            var clientIds = await _context.Clients.Select(c => c.Id).ToListAsync();
            var notifications = clientIds.Select(id => new Notification
            {
                PersonId = id,
                Message = message,
                Type = type,
                ScheduledFor = scheduledFor,
                CreatedAt = LocalNow
            }).ToList();
            foreach (var notification in notifications) AddScheduledEmail(notification, scheduledFor);
            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
            if (!scheduledFor.HasValue) await EmailAsync(clientIds, message);
        }

        // Creates a notification for every trainer.
        public async Task SendToAllTrainersAsync(string message, NotificationType type, DateTime? scheduledFor = null)
        {
            var trainerIds = await _context.Trainers.Select(t => t.Id).ToListAsync();
            var notifications = trainerIds.Select(id => new Notification
            {
                PersonId = id,
                Message = message,
                Type = type,
                ScheduledFor = scheduledFor,
                CreatedAt = LocalNow
            }).ToList();
            foreach (var notification in notifications) AddScheduledEmail(notification, scheduledFor);
            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
            if (!scheduledFor.HasValue) await EmailAsync(trainerIds, message);
        }

        // Creates a notification for every administrator.
        public async Task SendToAllAdminsAsync(string message, NotificationType type)
        {
            var adminIds = await _context.Admins.Select(a => a.Id).ToListAsync();
            var text = message.Length > 300 ? message[..300] : message;
            _context.Notifications.AddRange(adminIds.Select(id => new Notification
            {
                PersonId = id,
                Message = text,
                Type = type,
                CreatedAt = LocalNow
            }));
            await _context.SaveChangesAsync();
            await EmailAsync(adminIds, text);
        }

        private void AddScheduledEmail(Notification notification, DateTime? scheduledFor)
        {
            if (scheduledFor.HasValue)
                notification.EmailDelivery = new NotificationEmail { DueAtUtc = _appointmentTime.ToUtc(scheduledFor.Value) };
        }

        // Returns the user's notifications that are due and not dismissed.
        public async Task<List<Notification>> GetVisibleAsync(string personId, bool unreadOnly)
        {
            await GenerateAppointmentRemindersAsync(personId);
            var now = LocalNow;
            return await _context.Notifications
                .Where(n => n.PersonId == personId && !n.IsDismissed && (n.ScheduledFor == null || n.ScheduledFor <= now))
                .Where(n => !unreadOnly || !n.IsRead)
                .OrderByDescending(n => n.ScheduledFor ?? n.CreatedAt)
                .ToListAsync();
        }

        // Counts the user's unread notifications.
        public async Task<int> GetUnreadCountAsync(string personId)
        {
            await GenerateAppointmentRemindersAsync(personId);
            var now = LocalNow;
            return await _context.Notifications.CountAsync(n => n.PersonId == personId && !n.IsRead && !n.IsDismissed
                && (n.ScheduledFor == null || n.ScheduledFor <= now));
        }

        // Creates today's workout and meal reminders for a client if they do not exist yet.
        public async Task GenerateDailyRemindersAsync(string clientId)
        {
            var client = await _context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clientId);
            if (client == null) return;
            // Reminders follow the application's time zone, like the client's journals and plans.
            var now = _appointmentTime.ToLocal(_clock.GetUtcNow().UtcDateTime);
            var today = now.Date;
            var hour = now.Hour;
            // The dashboard and the background worker can run this at the same time, so one waits for the other.
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sp_getapplock @Resource = {"DailyReminders:" + clientId}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000");
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
            await transaction.CommitAsync();
            foreach (var reminder in reminders)
            {
                await EmailAsync(new[] { clientId }, reminder);
            }
        }

        private const string WorkoutReminderMessage = "You haven't logged a workout today. Keep your streak going!";
        private const string MealReminderMessage = "Don't forget to log your meals for today.";
        private const string WeightReminderMessage = "It's been over a week since your last weight check-in. Record a new measurement.";

        // Creates reminders for appointments that start soon.
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
                    reminder.CreatedAt = LocalNow;
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}
