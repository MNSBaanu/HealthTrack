using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthTrack.Models.ViewModels;

public class AssignmentRowViewModel
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string? TrainerId { get; set; }
    public string? TrainerName { get; set; }
    public DateTime? AssignedAtUtc { get; set; }
}

public class AssignmentListViewModel
{
    public List<AssignmentRowViewModel> Clients { get; set; } = new();
    public string? Search { get; set; }
    public bool UnassignedOnly { get; set; }
}

public class AssignmentForm
{
    [Required, StringLength(450)]
    public string ClientId { get; set; } = string.Empty;
    [Required(ErrorMessage = "Select a Trainer."), StringLength(450)]
    [Display(Name = "Trainer")]
    public string TrainerId { get; set; } = string.Empty;
    [StringLength(24)]
    public string? RowVersion { get; set; }
}

public class AssignmentEditViewModel : AssignmentForm
{
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string? CurrentTrainerName { get; set; }
    public List<SelectListItem> Trainers { get; set; } = new();
    public List<TrainerAssignmentHistory> History { get; set; } = new();
}

public class RemoveAssignmentForm
{
    [Required, StringLength(450)]
    public string ClientId { get; set; } = string.Empty;
    [Required, StringLength(24)]
    public string RowVersion { get; set; } = string.Empty;
}
