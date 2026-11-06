namespace HealthTrack.Models.ViewModels;

public static class ClientPlanStatus
{
    public static string Get(bool active, DateTime start, DateTime? end, DateTime today) =>
        !active ? "Inactive" : end.HasValue && end.Value.Date < today.Date ? "Ended" :
        start.Date > today.Date ? "Upcoming" : "Active";

    public static string Timing(DateTime start, DateTime? end, DateTime today)
    {
        static string Days(int count) => count == 1 ? "1 day" : $"{count} days";
        if (start.Date > today.Date) return $"Starts in {Days((start.Date - today.Date).Days)}";
        if (!end.HasValue) return "No end date";
        if (end.Value.Date < today.Date) return $"Ended {Days((today.Date - end.Value.Date).Days)} ago";
        return end.Value.Date == today.Date ? "Ends today" : $"{Days((end.Value.Date - today.Date).Days)} left";
    }
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
    public HashSet<int> CompletedItemIds { get; set; } = new();
    public HashSet<int> JournalItemIds { get; set; } = new();
    public DateTime? SelectedDate { get; set; }
    public DateTime DisplayDate => (SelectedDate ?? Today).Date;
    public DateTime WeekStart => DisplayDate.AddDays(-((int)DisplayDate.DayOfWeek + 6) % 7);
    public DateTime ScheduledDate(WorkoutPlanItem item) => item.DayOfWeek.HasValue
        ? WeekStart.AddDays(((int)item.DayOfWeek.Value + 6) % 7) : DisplayDate;
    public bool IsInPlanDates(WorkoutPlanItem item) => ScheduledDate(item) >= Plan.StartDate.Date
        && (!Plan.EndDate.HasValue || ScheduledDate(item) <= Plan.EndDate.Value.Date);
}

public class ClientDietPlanViewModel
{
    public DietPlan Plan { get; set; } = null!;
    public DateTime Today { get; set; }
    public HashSet<int> LoggedTodayMealIds { get; set; } = new();
    public int CaloriesToday { get; set; }
}
