using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services;

// Closes a client's waiting trainer request once an administrator has assigned a trainer.
public static class TrainerRequestWorkflow
{
    public static async Task FulfillAsync(HealthTrackDbContext context, string clientId, DateTime nowUtc)
    {
        var pending = await context.TrainerRequests
            .FirstOrDefaultAsync(r => r.ClientId == clientId && r.Status == TrainerRequestStatus.Pending);
        if (pending == null) return;
        pending.Status = TrainerRequestStatus.Fulfilled;
        pending.ResolvedAtUtc = nowUtc;
    }
}
