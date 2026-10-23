using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services;

public class DataRetentionWorker(IServiceScopeFactory scopes, IConfiguration configuration,
    ILogger<DataRetentionWorker> logger, TimeProvider clock) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var months = configuration.GetValue<int>("Retention:InactiveClientMonths");
        if (months <= 0) return;

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            do
            {
                try { await DeleteInactiveClientsAsync(months, stoppingToken); }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "Data retention run failed; retrying on the next cycle.");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    private async Task DeleteInactiveClientsAsync(int months, CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthTrackDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Person>>();
        var cutoff = clock.GetLocalNow().DateTime.AddMonths(-months);

        var inactive = await context.Clients
            .Where(c => c.CreatedAt.Year > 2000 && (c.LastLoginAt ?? c.CreatedAt) < cutoff)
            .ToListAsync(cancellationToken);
        foreach (var client in inactive)
        {
            var result = await users.DeleteAsync(client);
            if (!result.Succeeded)
            {
                logger.LogWarning("Could not delete inactive client {ClientId}", client.Id);
                continue;
            }
            context.AuditLogs.Add(new AuditLog
            {
                ActorName = "System",
                ActorRole = "System",
                Action = "Deleted",
                Entity = "InactiveClientAccount",
                SubjectClientId = client.Id,
                Detail = $"No sign-in for {months} months"
            });
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Deleted client {ClientId} after {Months} months of inactivity", client.Id, months);
        }
    }
}
