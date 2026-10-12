using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;

namespace HealthTrack.Services;

public class TrainerFeedbackService : ITrainerFeedbackService
{
    private readonly HealthTrackDbContext _context;
    private readonly INotificationService _notificationService;

    public TrainerFeedbackService(HealthTrackDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

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
}
