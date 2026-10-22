namespace HealthTrack.Models.ViewModels;

public static class ClientPlanStatus
{
    public static string Get(bool active, DateTime start, DateTime? end, DateTime today) =>
        !active ? "Inactive" : end.HasValue && end.Value.Date < today.Date ? "Ended" :
        start.Date > today.Date ? "Upcoming" : "Active";
}

public class ClientPlansViewModel
{
    public List<WorkoutPlan> WorkoutPlans { get; set; } = new();
    public List<DietPlan> DietPlans { get; set; } = new();
    public DateTime Today { get; set; }
}

public class ClientWorkoutPlanViewModel
{
    public WorkoutPlan Plan { get; set; } = null!;
    public DateTime Today { get; set; }
    public HashSet<int> DoneTodayItemIds { get; set; } = new();
}

public class ClientDietPlanViewModel
{
    public DietPlan Plan { get; set; } = null!;
    public DateTime Today { get; set; }
}
