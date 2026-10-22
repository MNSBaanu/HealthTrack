using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        [Range(50, 250)]
        [Display(Name = "Height (cm)")]
        public double? HeightCm { get; set; }

        [Required]
        [Range(1, 500)]
        [Display(Name = "Current Weight (kg)")]
        public double? WeightKg { get; set; }

        [StringLength(200)]
        [Display(Name = "Fitness Goal")]
        public string? FitnessGoal { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the privacy policy to create an account.")]
        [Display(Name = "Privacy policy")]
        public bool AcceptPrivacy { get; set; }
    }
}
