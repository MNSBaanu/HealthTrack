using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models;

// A notification an admin sent to a group of users.
public class AdminBroadcast
{
    public int Id { get; set; }
    [MaxLength(450)] public string SenderId { get; set; } = string.Empty;
    [MaxLength(100)] public string SenderName { get; set; } = string.Empty;
    [MaxLength(300)] public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledFor { get; set; }
    public int RecipientCount { get; set; }
    [MaxLength(400)] public string RecipientDescription { get; set; } = string.Empty;
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
