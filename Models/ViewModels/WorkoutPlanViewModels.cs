using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthTrack.Models.ViewModels;

public class WorkoutPlanItemInputModel
{
    public int Id { get; set; }

    public DayOfWeek? DayOfWeek { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Exercise")]
    public string ExerciseName { get; set; } = string.Empty;

    [Range(1, 100)]
    public int? Sets { get; set; }

    [Range(1, 500)]
    public int? Reps { get; set; }

    [Range(1, 300)]
    [Display(Name = "Duration (min)")]
    public int? DurationMinutes { get; set; }

    [Range(0, 600)]
    [Display(Name = "Rest (sec)")]
    public int? RestSeconds { get; set; }

    [StringLength(500)]
    public string? Instructions { get; set; }
}

public class WorkoutPlanFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please select a client.")]
    [Display(Name = "Client")]
    public string ClientId { get; set; } = string.Empty;

    public string? ClientName { get; set; }

    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Goal { get; set; }

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

    public List<WorkoutPlanItemInputModel> Items { get; set; } = new();
}

public class WorkoutPlanListViewModel
{
    public List<WorkoutPlan> Plans { get; set; } = new();
    public string? SelectedClientId { get; set; }
    public List<SelectListItem> AssignedClients { get; set; } = new();
}
