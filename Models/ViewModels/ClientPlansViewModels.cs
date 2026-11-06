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
    public List<TrainerFeedback> Feedback { get; set; } = new();
    public DateTime Today { get; set; }
    public HashSet<(int ItemId, DateTime Date)> CompletedPairs { get; set; } = new();
    public HashSet<(int ItemId, DateTime Date)> JournalPairs { get; set; } = new();
    public DateTime? SelectedDate { get; set; }
    public DateTime DisplayDate => (SelectedDate ?? Today).Date;
    public DateTime WeekStart => DisplayDate.AddDays(-((int)DisplayDate.DayOfWeek + 6) % 7);
    public IEnumerable<DateTime> WeekDays => Enumerable.Range(0, 7).Select(offset => WeekStart.AddDays(offset));
    public bool IsInPlan(DateTime day) => day.Date >= Plan.StartDate.Date && (!Plan.EndDate.HasValue || day.Date <= Plan.EndDate.Value.Date);

    // Exercises scheduled on a day: those set for that weekday, plus the every-day ones.
    public List<WorkoutPlanItem> ItemsOn(DateTime day) => !IsInPlan(day)
        ? new List<WorkoutPlanItem>()
        : Plan.Items.Where(i => !i.DayOfWeek.HasValue || i.DayOfWeek == day.DayOfWeek).OrderBy(i => i.Id).ToList();

    public bool IsCompleted(WorkoutPlanItem item, DateTime day) => CompletedPairs.Contains((item.Id, day.Date));
    public bool IsJournal(WorkoutPlanItem item, DateTime day) => JournalPairs.Contains((item.Id, day.Date));
}

public class ClientDietPlanViewModel
{
    public DietPlan Plan { get; set; } = null!;
    public List<TrainerFeedback> Feedback { get; set; } = new();
    public DateTime Today { get; set; }
    public HashSet<int> LoggedTodayMealIds { get; set; } = new();
    public int CaloriesToday { get; set; }
    public Dictionary<int, int> LoggedCaloriesToday { get; set; } = new();
    public Dictionary<DateTime, int> CaloriesByDate { get; set; } = new();
}
