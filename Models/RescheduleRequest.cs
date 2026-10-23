using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models;

// These numeric values are persisted. Keep them stable.
// Closed means the request no longer applies, e.g. the trainer cancelled or moved the appointment.
public enum RescheduleRequestStatus { Pending = 0, Accepted = 1, Declined = 2, Used = 3, Closed = 4 }

// A client's request to move an approved appointment. The client may only reschedule
// once the trainer accepts, and each accepted request can be used once.
public class RescheduleRequest
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;

    [Required, StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;

    public RescheduleRequestStatus Status { get; set; } = RescheduleRequestStatus.Pending;

    [StringLength(500)]
    public string? TrainerNote { get; set; }

    public DateTime RequestedAtUtc { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
}
