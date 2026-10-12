using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services;

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
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Could not refresh appointment reminders; retrying on the next cycle."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
