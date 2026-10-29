namespace HealthTrack.Models.ViewModels;

public class ClientTaskViewModel
{
    public int Id { get; set; }
    public int PlanId { get; set; }
    public string Kind { get; set; } = "Workout";
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public bool JournalEntry { get; set; }
    public bool CountsTowardsProgress => Kind != "Measurement";
    public bool CanToggle => Kind == "Workout" && !JournalEntry;
}
