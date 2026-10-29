using HealthTrack.Models.Data;
using HealthTrack.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services;

public class NotificationEmailDispatcher(HealthTrackDbContext context, IMailSender mail, TimeProvider clock,
    ILogger<NotificationEmailDispatcher> logger, AppointmentTime time)
{
    public async Task DispatchAsync(CancellationToken cancellationToken = default)
    {
        // Adopt future schedules from before the outbox existed, without replaying historical messages.
        var localNow = time.ToLocal(clock.GetUtcNow().UtcDateTime);
        var missing = await context.Notifications.AsNoTracking()
            .Where(n => n.ScheduledFor > localNow && !n.IsDismissed && n.EmailDelivery == null)
            .Select(n => new { n.NotificationId, n.ScheduledFor }).ToListAsync(cancellationToken);
        var recovered = missing.Select(n => new NotificationEmail
            { NotificationId = n.NotificationId, DueAtUtc = time.ToUtc(n.ScheduledFor!.Value) }).ToList();
        if (recovered.Count > 0)
        {
            context.NotificationEmails.AddRange(recovered);
            try { await context.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                foreach (var job in recovered) context.Entry(job).State = EntityState.Detached;
            }
        }
        if (!mail.IsConfigured) return;
        var now = clock.GetUtcNow().UtcDateTime;
        var candidates = await context.NotificationEmails.AsNoTracking()
            .Where(e => e.SentAtUtc == null && e.CancelledAtUtc == null && e.Attempts < 5
                && e.DueAtUtc <= now && (e.LeaseUntilUtc == null || e.LeaseUntilUtc <= now))
            .OrderBy(e => e.DueAtUtc).Select(e => e.NotificationId).Take(25).ToListAsync(cancellationToken);
        foreach (var id in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            now = clock.GetUtcNow().UtcDateTime;
            var lease = now.AddMinutes(5);
            var claimed = await context.NotificationEmails
                .Where(e => e.NotificationId == id && e.SentAtUtc == null && e.CancelledAtUtc == null
                    && e.Attempts < 5 && e.DueAtUtc <= now && (e.LeaseUntilUtc == null || e.LeaseUntilUtc <= now))
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.LeaseUntilUtc, lease)
                    .SetProperty(e => e.Attempts, e => e.Attempts + 1), cancellationToken);
            if (claimed == 0) continue;
            var delivery = await context.NotificationEmails.AsNoTracking().Include(e => e.Notification).ThenInclude(n => n.Person)
                .SingleOrDefaultAsync(e => e.NotificationId == id, cancellationToken);
            if (delivery == null) continue;
            var owner = context.NotificationEmails.Where(e => e.NotificationId == id && e.LeaseUntilUtc == lease);
            var person = delivery.Notification.Person;
            if (delivery.Notification.IsDismissed || person == null || !person.EmailNotificationsEnabled || string.IsNullOrWhiteSpace(person.Email))
            {
                await owner.ExecuteUpdateAsync(s => s.SetProperty(e => e.CancelledAtUtc, now)
                    .SetProperty(e => e.LeaseUntilUtc, (DateTime?)null), cancellationToken);
                continue;
            }
            try
            {
                await mail.SendNotificationAsync(person.Email, person.Name, delivery.Notification.Message, cancellationToken);
                await owner.ExecuteUpdateAsync(s => s.SetProperty(e => e.SentAtUtc, clock.GetUtcNow().UtcDateTime)
                    .SetProperty(e => e.LeaseUntilUtc, (DateTime?)null).SetProperty(e => e.LastError, (string?)null), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Notification email {NotificationId} failed; attempt {Attempt}", id, delivery.Attempts);
                await owner.ExecuteUpdateAsync(s => s.SetProperty(e => e.DueAtUtc, clock.GetUtcNow().UtcDateTime.AddMinutes(Math.Pow(2, delivery.Attempts)))
                    .SetProperty(e => e.LeaseUntilUtc, (DateTime?)null)
                    .SetProperty(e => e.LastError, "Email delivery failed. Will retry up to five attempts."), cancellationToken);
            }
        }
    }
}
