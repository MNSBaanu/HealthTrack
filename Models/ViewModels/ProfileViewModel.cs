using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models.ViewModels
{
    public class ProfileViewModel
    {
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        [Range(50, 250)]
        [Display(Name = "Height (cm)")]
        public double? HeightCm { get; set; }

        [StringLength(200)]
        [Display(Name = "Fitness Goal")]
        public string? FitnessGoal { get; set; }

        [Range(20, 300)]
        [Display(Name = "Target weight (kg)")]
        public double? TargetWeightKg { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Target date")]
        public DateTime? TargetDate { get; set; }

        [Display(Name = "Email notifications")]
        public bool EmailNotificationsEnabled { get; set; }

        [Display(Name = "Workout reminder")]
        public bool WorkoutReminderEnabled { get; set; }

        [Range(0, 23)]
        public int WorkoutReminderHour { get; set; }

        [Display(Name = "Meal reminder")]
        public bool MealReminderEnabled { get; set; }

        [Range(0, 23)]
        public int MealReminderHour { get; set; }

        [Display(Name = "Weight check-in reminder")]
        public bool WeightReminderEnabled { get; set; }

        [Range(0, 23)]
        public int WeightReminderHour { get; set; }

        [Display(Name = "Profile photo")]
        public IFormFile? Photo { get; set; }

        [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
        public bool HasPhoto { get; set; }
    }
}
