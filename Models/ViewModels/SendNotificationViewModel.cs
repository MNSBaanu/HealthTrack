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

        [EnumDataType(typeof(NotificationType))]
        public NotificationType Type { get; set; } = NotificationType.General;

        [Display(Name = "Scheduled For")]
        public DateTime? ScheduledFor { get; set; }
    }

    public record BroadcastHistoryItem(DateTime SentAt, NotificationType Type, string Message, DateTime? ScheduledFor,
        int Recipients, string? RecipientName, string SenderName, string Status, int EmailsSent, int EmailsPending, int EmailsFailed);
}
