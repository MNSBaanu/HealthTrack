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

public class RescheduleForm
{
    [Required(ErrorMessage = "Choose an available appointment time."), Range(1, int.MaxValue)]
    [Display(Name = "New appointment time")]
    public int? NewSlotId { get; set; }
}

public class ReschedulePageViewModel : RescheduleForm
{
    public int AppointmentId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime CurrentStartLocal { get; set; }
    public DateTime CurrentEndLocal { get; set; }
    public List<AvailableSlotViewModel> AvailableSlots { get; set; } = new();
    public string TimeZoneLabel { get; set; } = string.Empty;
}

public class BookingPageViewModel : BookingForm
{
    public int? ProviderId { get; set; }
    public ConsultationProviderKind? Kind { get; set; }
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

    // Latest reschedule request for an approved appointment, if any.
    public RescheduleRequestStatus? RescheduleStatus { get; set; }
    public string? RescheduleReason { get; set; }
    public string? RescheduleTrainerNote { get; set; }
    public bool CanRequestReschedule { get; set; }
    public bool CanReschedule { get; set; }
}

public class RescheduleRequestForm
{
    [Required(ErrorMessage = "Explain why you need to reschedule.")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "The reason must be between 10 and 500 characters.")]
    [Display(Name = "Reason for rescheduling")]
    public string? Reason { get; set; }
}

public class RescheduleRequestPageViewModel : RescheduleRequestForm
{
    public AppointmentViewModel Appointment { get; set; } = new();
    public string TimeZoneLabel { get; set; } = string.Empty;
}

public class AppointmentListViewModel
{
    public List<AppointmentViewModel> Appointments { get; set; } = new();
    public string TimeZoneLabel { get; set; } = string.Empty;
}
