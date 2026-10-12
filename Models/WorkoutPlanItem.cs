using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models;

public class WorkoutPlanItem
{
    public int Id { get; set; }

    public int WorkoutPlanId { get; set; }
    public WorkoutPlan? WorkoutPlan { get; set; }

    public DayOfWeek? DayOfWeek { get; set; }

    [Required]
    [StringLength(100)]
    public string ExerciseName { get; set; } = string.Empty;

    [Range(1, 100)]
    public int? Sets { get; set; }

    [Range(1, 500)]
    public int? Reps { get; set; }

    [Range(1, 300)]
    public int? DurationMinutes { get; set; }

    [Range(0, 600)]
    public int? RestSeconds { get; set; }

    [StringLength(500)]
    public string? Instructions { get; set; }
}
