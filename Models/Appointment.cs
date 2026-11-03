using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models;

// These numeric values are persisted. Keep them stable across modules.
public enum AppointmentStatus { Pending = 0, Approved = 1, Cancelled = 2, Completed = 3 }

public class Appointment
{
    public int Id { get; set; }
    [Required]
    public string ClientId { get; set; } = string.Empty;
    public Client Client { get; set; } = null!;
    public int SlotId { get; set; }
    public AvailabilitySlot Slot { get; set; } = null!;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public DateTime RequestedAtUtc { get; set; }
    [StringLength(2000)]
    public string? Notes { get; set; }

    [Range(1, 5)]
    public int? Rating { get; set; }

    [StringLength(500)]
    public string? RatingComment { get; set; }

    public DateTime? RatedAtUtc { get; set; }
    public bool RatingHidden { get; set; }
}
