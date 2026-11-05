using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services;

// Background worker that creates reminders for upcoming appointments.
public class AppointmentReminderWorker(IServiceScopeFactory scopes,
    ILogger<AppointmentReminderWorker> logger, TimeProvider clock) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<HealthTrackDbContext>();
                var now = clock.GetUtcNow().UtcDateTime;
                var cutoff = now.AddHours(24);
                var clients = await context.Appointments
                    .Where(a => a.Status == AppointmentStatus.Approved && a.Slot.StartUtc > now && a.Slot.StartUtc <= cutoff)
                    .Select(a => a.ClientId)
                    .Union(context.Notifications.Where(n => n.AppointmentId != null).Select(n => n.PersonId))
                    .Distinct().ToListAsync(stoppingToken);
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
                foreach (var clientId in clients)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    await notifications.GenerateAppointmentRemindersAsync(clientId);
                }

                // Trainers get one reminder per consultation; the time is in the message so a moved consultation is reminded again.
                var time = scope.ServiceProvider.GetRequiredService<AppointmentTime>();
                var trainerSessions = await context.Appointments.AsNoTracking()
                    .Where(a => a.Status == AppointmentStatus.Approved && a.Slot.StartUtc > now && a.Slot.StartUtc <= cutoff && a.Slot.Provider.PersonId != null)
                    .Select(a => new { TrainerId = a.Slot.Provider.PersonId!, ClientName = a.Client.Name, a.Slot.StartUtc })
                    .ToListAsync(stoppingToken);
                foreach (var session in trainerSessions)
                {
                    var message = $"Consultation reminder: {session.ClientName} on {time.ToLocal(session.StartUtc):ddd, d MMM yyyy HH:mm} ({time.Label}).";
                    if (!await context.Notifications.AnyAsync(n => n.PersonId == session.TrainerId && n.Message == message, stoppingToken))
                        await notifications.SendAsync(session.TrainerId, message, NotificationType.Appointment);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Could not refresh appointment reminders; retrying on the next cycle."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
