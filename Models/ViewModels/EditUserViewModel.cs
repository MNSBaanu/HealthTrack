using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models.ViewModels
{
    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

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

        [StringLength(200)]
        public string? Specialty { get; set; }

        public bool IsTrainer { get; set; }
    }
}
