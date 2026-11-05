using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    // A recorded data change or user action.
    public class AuditLog
    {
        public long Id { get; set; }

        [StringLength(450)]
        public string ActorId { get; set; } = string.Empty;

        [StringLength(256)]
        public string ActorName { get; set; } = string.Empty;

        [StringLength(20)]
        public string ActorRole { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(60)]
        public string Entity { get; set; } = string.Empty;

        [StringLength(450)]
        public string? SubjectClientId { get; set; }

        [StringLength(300)]
        public string? Detail { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
