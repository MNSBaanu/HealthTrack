using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthTrack.Models.ViewModels;

public class TrainerRequestForm
{
    [StringLength(450)]
    [Display(Name = "Preferred trainer (optional)")]
    public string? PreferredTrainerId { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }
}

public class TrainerRequestPageViewModel : TrainerRequestForm
{
    public string? CurrentTrainerName { get; set; }
    public List<SelectListItem> Trainers { get; set; } = new();
}

public class TrainerRequestRowViewModel
{
    public int Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string? CurrentTrainerName { get; set; }
    public string? PreferredTrainerId { get; set; }
    public string? PreferredTrainerName { get; set; }
    public string? Note { get; set; }
    public bool IsChange { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class DeclineTrainerRequestForm
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [StringLength(300)]
    public string? Message { get; set; }
}
