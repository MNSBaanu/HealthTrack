using HealthTrack.Models;
using HealthTrack.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthTrack.Services;

public interface ITrainerPlanService
{
    Task<bool> IsClientAssignedToTrainerAsync(string trainerId, string clientId);
    Task<List<SelectListItem>> GetAssignedClientSelectListAsync(string trainerId);
    Task<WorkoutPlan?> GetWorkoutPlanByIdAsync(int planId, string trainerId);
    Task<DietPlan?> GetDietPlanByIdAsync(int planId, string trainerId);
    Task<int> SaveWorkoutPlanAsync(string trainerId, WorkoutPlanFormViewModel model);
    Task<int> SaveDietPlanAsync(string trainerId, DietPlanFormViewModel model);
    Task<bool> ToggleWorkoutPlanStatusAsync(int planId, string trainerId);
    Task<bool> ToggleDietPlanStatusAsync(int planId, string trainerId);
}
