using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    // A workout logged by a client.
    public class WorkoutLog
    {
        [Key]
        public int WorkoutLogId { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;

        public Client? Client { get; set; }

        [Required]
        [StringLength(100)]
        public string Activity { get; set; } = string.Empty;

        [Range(1, 600)]
        public int DurationMinutes { get; set; }

        [Range(0, 10000)]
        public int CaloriesBurned { get; set; }

        public DateTime LoggedAt { get; set; } = DateTime.Now;

        public int? WorkoutPlanItemId { get; set; }

        public WorkoutPlanItem? WorkoutPlanItem { get; set; }
    }
}
