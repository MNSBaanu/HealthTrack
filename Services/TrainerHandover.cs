using HealthTrack.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services;

// Hands a client's plans to their new trainer and drops check-ins that earlier trainers are still waiting on.
public static class TrainerHandover
{
    public static async Task MoveClientAsync(HealthTrackDbContext context, string clientId, string? newTrainerId)
    {
        if (newTrainerId != null)
        {
            await context.WorkoutPlans.Where(p => p.ClientId == clientId && p.TrainerId != newTrainerId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.TrainerId, newTrainerId));
            await context.DietPlans.Where(p => p.ClientId == clientId && p.TrainerId != newTrainerId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.TrainerId, newTrainerId));
        }
        await context.WeeklyCheckIns.Where(c => c.ClientId == clientId && c.TrainerId != newTrainerId && c.RespondedAtUtc == null)
            .ExecuteDeleteAsync();
    }
}
