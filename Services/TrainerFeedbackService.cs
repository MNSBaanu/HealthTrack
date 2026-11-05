using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services;

// Saves, edits and deletes trainer feedback, limited to the trainer's own clients.
public class TrainerFeedbackService : ITrainerFeedbackService
{
    private readonly HealthTrackDbContext _context;
    private readonly INotificationService _notificationService;

    public TrainerFeedbackService(HealthTrackDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    // Saves new feedback and notifies the client.
    public async Task<int> AddFeedbackAsync(string trainerId, TrainerFeedbackFormViewModel model)
    {
        var feedback = new TrainerFeedback
        {
            TrainerId = trainerId,
            ClientId = model.ClientId,
            Title = model.Title.Trim(),
            Comments = model.Comments.Trim(),
            Category = model.Category,
            CreatedAtUtc = DateTime.UtcNow,
            IsReadByClient = false
        };

        _context.TrainerFeedbacks.Add(feedback);
        await _context.SaveChangesAsync();

        await _notificationService.SendAsync(
            model.ClientId,
            $"New Trainer Feedback received: '{feedback.Title}'",
            NotificationType.General);

        return feedback.Id;
    }

    // Loads feedback the trainer is allowed to edit.
    public async Task<TrainerFeedbackFormViewModel?> GetFeedbackForEditAsync(string trainerId, int feedbackId)
    {
        var feedback = await _context.TrainerFeedbacks
            .Include(f => f.Client)
            .FirstOrDefaultAsync(f => f.Id == feedbackId && f.TrainerId == trainerId && f.ClientReply == null);

        if (feedback == null) return null;

        return new TrainerFeedbackFormViewModel
        {
            ClientId = feedback.ClientId,
            ClientName = feedback.Client?.Name,
            Title = feedback.Title,
            Comments = feedback.Comments,
            Category = feedback.Category,
            AssignedClients = await GetAssignedClientsAsync(trainerId)
        };
    }

    // Saves changes to feedback.
    public async Task<bool> UpdateFeedbackAsync(string trainerId, int feedbackId, TrainerFeedbackFormViewModel model)
    {
        var feedback = await _context.TrainerFeedbacks
            .FirstOrDefaultAsync(f => f.Id == feedbackId && f.TrainerId == trainerId && f.ClientReply == null);

        if (feedback == null) return false;

        feedback.Title = model.Title.Trim();
        feedback.Comments = model.Comments.Trim();
        feedback.Category = model.Category;

        await _context.SaveChangesAsync();
        return true;
    }

    // Deletes feedback.
    public async Task<bool> DeleteFeedbackAsync(string trainerId, int feedbackId)
    {
        var feedback = await _context.TrainerFeedbacks
            .FirstOrDefaultAsync(f => f.Id == feedbackId && f.TrainerId == trainerId);

        if (feedback == null) return false;

        _context.TrainerFeedbacks.Remove(feedback);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<List<SelectListItem>> GetAssignedClientsAsync(string trainerId)
    {
        var clients = await _context.ClientTrainerAssignments
            .Where(a => a.TrainerId == trainerId)
            .Include(a => a.Client)
            .Select(a => a.Client)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return clients.Select(c => new SelectListItem
        {
            Value = c.Id,
            Text = c.Name
        }).ToList();
    }
}
