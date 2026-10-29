namespace HealthTrack.Services;

public class NotificationEmailWorker(IServiceScopeFactory scopes, ILogger<NotificationEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        try
        {
            do
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<NotificationEmailDispatcher>().DispatchAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                { logger.LogError(ex, "Scheduled email processing failed; retrying next minute."); }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
