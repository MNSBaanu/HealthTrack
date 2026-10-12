using HealthTrack.Models.ViewModels;

namespace HealthTrack.Services;

public interface ITrainerFeedbackService
{
    Task<int> AddFeedbackAsync(string trainerId, TrainerFeedbackFormViewModel model);
}
