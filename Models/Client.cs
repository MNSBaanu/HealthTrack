using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    public class Client : Person
    {
        [Range(50, 250)]
        public double HeightCm { get; set; }

        [StringLength(200)]
        public string? FitnessGoal { get; set; }

        public byte[]? ProfilePhoto { get; set; }

        [StringLength(32)]
        public string? ProfilePhotoContentType { get; set; }

        public ICollection<HealthMetric> HealthMetrics { get; set; } = new List<HealthMetric>();

        public ICollection<WorkoutLog> WorkoutLogs { get; set; } = new List<WorkoutLog>();

        public ICollection<MealLog> MealLogs { get; set; } = new List<MealLog>();
        public ClientTrainerAssignment? TrainerAssignment { get; set; }

        [NotMapped]
        public override string DashboardController => "Client";

        [NotMapped]
        public override string RoleDisplayName => "Client";
    }
}
