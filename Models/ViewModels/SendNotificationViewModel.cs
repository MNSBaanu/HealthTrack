using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models.ViewModels
{
    public class SendNotificationViewModel
    {
        [Display(Name = "Recipient")]
        public string? RecipientId { get; set; }

        [Required]
        [StringLength(300)]
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; } = NotificationType.General;

        [Display(Name = "Scheduled For")]
        public DateTime? ScheduledFor { get; set; }
    }
}
