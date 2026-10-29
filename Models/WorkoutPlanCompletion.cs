namespace HealthTrack.Models;

public class WorkoutPlanCompletion
{
    public int WorkoutPlanItemId { get; set; }
    public WorkoutPlanItem WorkoutPlanItem { get; set; } = null!;
    public string ClientId { get; set; } = string.Empty;
    public Client Client { get; set; } = null!;
    public DateTime CompletedOn { get; set; }
}
