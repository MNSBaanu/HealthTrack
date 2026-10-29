using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public string PersonId { get; set; } = string.Empty;

        public Person? Person { get; set; }

        [Required]
        [StringLength(300)]
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ScheduledFor { get; set; }
        // Set only for automatic consultation reminders; manual messages stay independent.
        public int? AppointmentId { get; set; }
        public Appointment? Appointment { get; set; }
        public DateTime? AppointmentStartUtc { get; set; }
        public bool IsDismissed { get; set; }
        public int? AdminBroadcastId { get; set; }
        public AdminBroadcast? AdminBroadcast { get; set; }
        public NotificationEmail? EmailDelivery { get; set; }
    }
}
