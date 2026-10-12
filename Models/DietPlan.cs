using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models;

public class DietPlan
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

    [Range(500, 10000)]
    public int DailyCalorieTarget { get; set; }

    [Range(0, 500)]
    public int? TargetProteinGrams { get; set; }

    [Range(0, 1000)]
    public int? TargetCarbsGrams { get; set; }

    [Range(0, 300)]
    public int? TargetFatGrams { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<DietPlanMeal> Meals { get; set; } = new List<DietPlanMeal>();
}
