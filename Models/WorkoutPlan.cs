using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models;

// A trainer's workout plan for a client.
public class WorkoutPlan
{
    public int Id { get; set; }

    [Required]
    public string TrainerId { get; set; } = string.Empty;
    public Trainer? Trainer { get; set; }

    [Required]
    public string ClientId { get; set; } = string.Empty;
    public Client? Client { get; set; }

    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Goal { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<WorkoutPlanItem> Items { get; set; } = new List<WorkoutPlanItem>();
}
