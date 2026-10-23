using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models.ViewModels;

public class AddAvailabilitySlotForm
{
    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Start Time")]
    public DateTime StartLocal { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "End Time")]
    public DateTime EndLocal { get; set; }
}

public class TrainerAppointmentItemViewModel
{
    public int Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public DateTime StartLocal { get; set; }
    public DateTime EndLocal { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime RequestedAtLocal { get; set; }
}

public class TrainerAvailabilitySlotViewModel
{
    public int Id { get; set; }
    public DateTime StartLocal { get; set; }
    public DateTime EndLocal { get; set; }
    public bool IsBooked { get; set; }
}

public class TrainerScheduleViewModel
{
    public string ProviderName { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public string TimeZoneLabel { get; set; } = string.Empty;

    public AddAvailabilitySlotForm NewSlotForm { get; set; } = new();

    public List<TrainerAvailabilitySlotViewModel> UpcomingSlots { get; set; } = new();
    public List<TrainerAppointmentItemViewModel> PendingAppointments { get; set; } = new();
    public List<TrainerAppointmentItemViewModel> UpcomingApprovedAppointments { get; set; } = new();
    public List<TrainerAppointmentItemViewModel> PastOrCancelledAppointments { get; set; } = new();
}
