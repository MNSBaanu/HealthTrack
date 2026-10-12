using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthTrack.Models.ViewModels;

public class DietPlanMealInputModel
{
    public int Id { get; set; }

    [Required]
    public MealType MealType { get; set; }

    [Required]
    [StringLength(300)]
    [Display(Name = "Recommended Foods")]
    public string FoodRecommendations { get; set; } = string.Empty;

    [Range(0, 5000)]
    [Display(Name = "Est. Calories")]
    public int? ApproximateCalories { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }
}

public class DietPlanFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please select a client.")]
    [Display(Name = "Client")]
    public string ClientId { get; set; } = string.Empty;

    public string? ClientName { get; set; }

    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Range(500, 10000)]
    [Display(Name = "Daily Calorie Target (kcal)")]
    public int DailyCalorieTarget { get; set; } = 2000;

    [Range(0, 500)]
    [Display(Name = "Protein (g)")]
    public int? TargetProteinGrams { get; set; }

    [Range(0, 1000)]
    [Display(Name = "Carbs (g)")]
    public int? TargetCarbsGrams { get; set; }

    [Range(0, 300)]
    [Display(Name = "Fat (g)")]
    public int? TargetFatGrams { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "End Date")]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Active Plan")]
    public bool IsActive { get; set; } = true;

    public List<SelectListItem> AssignedClients { get; set; } = new();

    public List<DietPlanMealInputModel> Meals { get; set; } = new();
}

public class DietPlanListViewModel
{
    public List<DietPlan> Plans { get; set; } = new();
    public string? SelectedClientId { get; set; }
    public List<SelectListItem> AssignedClients { get; set; } = new();
}
