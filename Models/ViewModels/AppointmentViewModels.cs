using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models.ViewModels;

public class BookingForm
{
    [Required(ErrorMessage = "Choose an available appointment time."), Range(1, int.MaxValue)]
    [Display(Name = "Available date and time")]
    public int? SlotId { get; set; }
    [StringLength(2000), Display(Name = "Consultation notes (optional)")]
    public string? Notes { get; set; }
}

public class BookingPageViewModel : BookingForm
{
    public int? ProviderId { get; set; }
    public List<ConsultationProvider> Providers { get; set; } = new();
    public List<AvailableSlotViewModel> Slots { get; set; } = new();
    public string TimeZoneLabel { get; set; } = string.Empty;
}

public record AvailableSlotViewModel(int Id, string ProviderName, DateTime Start, DateTime End);

public class AppointmentViewModel
{
    public int Id { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderKind { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Notes { get; set; }
}

public class AppointmentListViewModel
{
    public List<AppointmentViewModel> Appointments { get; set; } = new();
    public string TimeZoneLabel { get; set; } = string.Empty;
}
