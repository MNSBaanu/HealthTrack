using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthTrack.Models.ViewModels;

public class TrainerFeedbackFormViewModel
{
    [Required(ErrorMessage = "Please select a client.")]
    public string ClientId { get; set; } = string.Empty;

    public string? ClientName { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    [Display(Name = "Feedback & Guidance")]
    public string Comments { get; set; } = string.Empty;

    public FeedbackCategory Category { get; set; } = FeedbackCategory.General;

    public List<SelectListItem> AssignedClients { get; set; } = new();
}

public class TrainerFeedbackItemViewModel
{
    public int Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public FeedbackCategory Category { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsReadByClient { get; set; }
}
