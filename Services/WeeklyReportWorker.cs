using HealthTrack.Models;
using HealthTrack.Models.Data;

namespace HealthTrack.Services;

public class WeeklyReportWorker(IServiceScopeFactory scopes, AppointmentTime time, TimeProvider clock,
    ILogger<WeeklyReportWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<HealthTrackDbContext>();
                var settings = await SystemSettings.LoadAsync(context, stoppingToken);
                var now = time.ToLocal(clock.GetUtcNow().UtcDateTime);
                var lastSent = settings.WeeklyReportSentAtUtc is DateTime sent ? time.ToLocal(sent).Date : (DateTime?)null;
                if (settings.WeeklyReportEnabled && now.DayOfWeek == DayOfWeek.Monday && now.Hour >= 8 && lastSent != now.Date
                    && scope.ServiceProvider.GetRequiredService<IMailSender>().IsConfigured)
                {
                    var count = await scope.ServiceProvider.GetRequiredService<WeeklyReportSender>().SendAsync(stoppingToken);
                    logger.LogInformation("Weekly report emailed to {Count} admins", count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Could not send the weekly report; retrying on the next cycle."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
