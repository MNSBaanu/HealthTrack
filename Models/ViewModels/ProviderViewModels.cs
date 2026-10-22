using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models.ViewModels;

public class ProviderForm
{
    public int? Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Specialty { get; set; }
}

public class ProviderSlotsViewModel
{
    public ConsultationProvider Provider { get; set; } = null!;
    public List<AvailableSlotViewModel> Slots { get; set; } = new();
    public AddAvailabilitySlotForm Form { get; set; } = new();
    public string TimeZoneLabel { get; set; } = string.Empty;
}
