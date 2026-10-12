using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models;

public class DietPlanMeal
{
    public int Id { get; set; }

    public int DietPlanId { get; set; }
    public DietPlan? DietPlan { get; set; }

    public MealType MealType { get; set; }

    [Required]
    [StringLength(300)]
    public string FoodRecommendations { get; set; } = string.Empty;

    [Range(0, 5000)]
    public int? ApproximateCalories { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }
}
