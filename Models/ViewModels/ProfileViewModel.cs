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

        [Display(Name = "Profile photo")]
        public IFormFile? Photo { get; set; }

        [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
        public bool HasPhoto { get; set; }
    }
}
