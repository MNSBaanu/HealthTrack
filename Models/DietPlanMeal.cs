using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models;

// A meal within a diet plan.
public class DietPlanMeal
{
    public int Id { get; set; }

    public int DietPlanId { get; set; }
    public DietPlan? DietPlan { get; set; }

    public MealType MealType { get; set; }

    public DayOfWeek? Day { get; set; }

    // Returns true when the plan is active on the given date.
    public bool CanLogOn(DateTime date) => DietPlan is { IsActive: true } plan
        && date.Date >= plan.StartDate.Date
        && (!plan.EndDate.HasValue || date.Date <= plan.EndDate.Value.Date)
        && (!Day.HasValue || Day.Value == date.DayOfWeek);

    public TimeOnly? Time { get; set; }

    [Required]
    [StringLength(300)]
    public string FoodRecommendations { get; set; } = string.Empty;

    [Range(0, 5000)]
    public int? ApproximateCalories { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }
}
