namespace HealthTrack.Models.ViewModels;

public class TrainerReportsViewModel
{
    public string TrainerName { get; set; } = string.Empty;
    public int TotalClients { get; set; }
    public int ActiveClientsPastWeek { get; set; }
    public int TotalWorkoutsLogged { get; set; }
    public int TotalMealsLogged { get; set; }
    public double AverageWorkoutsPerClient { get; set; }

    public List<ClientPerformanceViewModel> ClientPerformances { get; set; } = new();
    public List<ActivityStatViewModel> PopularActivities { get; set; } = new();
}
