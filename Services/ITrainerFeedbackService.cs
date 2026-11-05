using HealthTrack.Models.ViewModels;

namespace HealthTrack.Services;

// Manages feedback that trainers send to clients.
public interface ITrainerFeedbackService
{
    Task<int> AddFeedbackAsync(string trainerId, TrainerFeedbackFormViewModel model);
    Task<TrainerFeedbackFormViewModel?> GetFeedbackForEditAsync(string trainerId, int feedbackId);
    Task<bool> UpdateFeedbackAsync(string trainerId, int feedbackId, TrainerFeedbackFormViewModel model);
    Task<bool> DeleteFeedbackAsync(string trainerId, int feedbackId);
}
