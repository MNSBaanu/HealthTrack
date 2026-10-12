namespace HealthTrack.Models.ViewModels;

public class TrainerDashboardViewModel
{
    public string TrainerName { get; set; } = string.Empty;
    public int TotalAssignedClients { get; set; }
    public int PendingAppointmentsCount { get; set; }
    public int ActiveWorkoutPlansCount { get; set; }
    public int ActiveDietPlansCount { get; set; }

    public List<TrainerClientSummaryViewModel> AssignedClients { get; set; } = new();
    public List<TrainerAppointmentItemViewModel> PendingAppointments { get; set; } = new();
    public List<ClientRecentActivityViewModel> RecentActivities { get; set; } = new();
    public List<TrainerFeedbackItemViewModel> RecentFeedbacks { get; set; } = new();
}
