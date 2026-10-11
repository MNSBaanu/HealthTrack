namespace HealthTrack.Models.ViewModels
{
    public class ClientDashboardViewModel
    {
        public Client Client { get; set; } = null!;
        public HealthMetric? LatestMetric { get; set; }
        public double? WeightChangeKg { get; set; }
        public int WorkoutsThisWeek { get; set; }
        public int MinutesThisWeek { get; set; }
        public int CaloriesBurnedThisWeek { get; set; }
        public int CaloriesConsumedToday { get; set; }
        public List<WorkoutLog> RecentWorkouts { get; set; } = new();
        public List<MealLog> RecentMeals { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
        public List<AppointmentViewModel> UpcomingAppointments { get; set; } = new();
        public string AppointmentTimeZone { get; set; } = string.Empty;
    }
}
