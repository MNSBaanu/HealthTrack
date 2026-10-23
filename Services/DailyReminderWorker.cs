using HealthTrack.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services;

public class DailyReminderWorker(IServiceScopeFactory scopes,
    ILogger<DailyReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<HealthTrackDbContext>();
                var clients = await context.Clients.AsNoTracking().Select(c => c.Id).ToListAsync(stoppingToken);
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
                foreach (var clientId in clients)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    await notifications.GenerateDailyRemindersAsync(clientId);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Could not refresh daily reminders; retrying on the next cycle."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
