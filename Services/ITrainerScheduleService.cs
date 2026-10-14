using HealthTrack.Models;
using HealthTrack.Models.ViewModels;

namespace HealthTrack.Services;

public interface ITrainerScheduleService
{
    Task<ConsultationProvider> GetOrCreateTrainerProviderAsync(string trainerId);
    Task<TrainerScheduleViewModel> GetScheduleAsync(string trainerId);
    Task<(bool Success, string? Error)> AddAvailabilitySlotAsync(string trainerId, DateTime startLocal, DateTime endLocal);
    Task<(bool Success, string? Error)> DeleteSlotAsync(string trainerId, int slotId);
    Task<(bool Success, string? Error)> ApproveAppointmentAsync(string trainerId, int appointmentId);
    Task<(bool Success, string? Error)> CancelAppointmentAsync(string trainerId, int appointmentId, string? reason);
    Task<(bool Success, string? Error)> RescheduleAppointmentAsync(string trainerId, int appointmentId, int newSlotId);
    Task<ReschedulePageViewModel?> LoadReschedulePageAsync(string trainerId, int appointmentId);
}
