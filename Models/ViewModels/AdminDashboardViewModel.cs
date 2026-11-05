namespace HealthTrack.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalClients { get; set; }
        public int TotalAdmins { get; set; }
        public int NewUsersThisWeek { get; set; }
        public int ActiveUsersThisWeek { get; set; }
        public int LockedUsers { get; set; }
        public int WorkoutLogsThisWeek { get; set; }
        public int MealLogsThisWeek { get; set; }
        public int MetricsThisWeek { get; set; }
        public int AppointmentsThisWeek { get; set; }
        public int NotificationsThisWeek { get; set; }
        public int TrainerPlansThisWeek { get; set; }
        public int FeedbackThisWeek { get; set; }
        public List<ActivityStatViewModel> PopularActivities { get; set; } = new();
        public int TotalTrainers { get; set; }
        public int AssignedClients { get; set; }
        public int UnassignedClients => TotalClients - AssignedClients;
        public int TrainersWithClients { get; set; }
        public List<ClientTrainerAssignment> RecentAssignments { get; set; } = new();
    }
}
