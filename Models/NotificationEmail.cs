using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models;

// Email delivery state for a notification.
public class NotificationEmail
{
    public int NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;
    public DateTime DueAtUtc { get; set; }
    public int Attempts { get; set; }
    public DateTime? LeaseUntilUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    [MaxLength(300)] public string? LastError { get; set; }
}
