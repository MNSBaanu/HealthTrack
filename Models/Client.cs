using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    // Client user with health profile details; inherits from Person.
    public class Client : Person
    {
        [Range(50, 250)]
        public double HeightCm { get; set; }

        [StringLength(200)]
        public string? FitnessGoal { get; set; }

        public byte[]? ProfilePhoto { get; set; }

        [StringLength(32)]
        public string? ProfilePhotoContentType { get; set; }

        [Range(20, 300)]
        public double? TargetWeightKg { get; set; }

        public DateTime? TargetDate { get; set; }

        public bool WorkoutReminderEnabled { get; set; } = true;

        [Range(0, 23)]
        public int WorkoutReminderHour { get; set; } = 8;

        public bool MealReminderEnabled { get; set; } = true;

        [Range(0, 23)]
        public int MealReminderHour { get; set; } = 8;

        public bool WeightReminderEnabled { get; set; } = true;

        [Range(0, 23)]
        public int WeightReminderHour { get; set; } = 8;

        public ICollection<HealthMetric> HealthMetrics { get; set; } = new List<HealthMetric>();

        public ICollection<WorkoutLog> WorkoutLogs { get; set; } = new List<WorkoutLog>();

        public ICollection<MealLog> MealLogs { get; set; } = new List<MealLog>();
        public ClientTrainerAssignment? TrainerAssignment { get; set; }
        public ICollection<WorkoutPlan> WorkoutPlans { get; set; } = new List<WorkoutPlan>();
        public ICollection<DietPlan> DietPlans { get; set; } = new List<DietPlan>();
        public ICollection<TrainerFeedback> Feedbacks { get; set; } = new List<TrainerFeedback>();

        [NotMapped]
        public override string DashboardController => "Client";

        [NotMapped]
        public override string RoleDisplayName => "Client";
    }
}
