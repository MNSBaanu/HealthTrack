using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services;

// Queries for client and trainer assignments.
public class TrainerAssignmentQueries(HealthTrackDbContext context)
{
    // Returns every client with their current trainer.
    public IQueryable<AssignmentRowViewModel> AllClients() => context.Clients.AsNoTracking()
        .Select(c => new AssignmentRowViewModel
        {
            ClientId = c.Id, ClientName = c.Name, ClientEmail = c.Email ?? string.Empty,
            TrainerId = c.TrainerAssignment == null ? null : c.TrainerAssignment.TrainerId,
            TrainerName = c.TrainerAssignment == null ? null : c.TrainerAssignment.Trainer.Name,
            AssignedAtUtc = c.TrainerAssignment == null ? null : c.TrainerAssignment.AssignedAtUtc
        });

    // Reusable query for the Trainer module. Call with the authenticated Trainer ID.
    // Returns the clients assigned to a trainer.
    public Task<List<AssignmentRowViewModel>> ForTrainerAsync(string trainerId) =>
        AllClients().Where(c => c.TrainerId == trainerId).OrderBy(c => c.ClientName).ToListAsync();
}
