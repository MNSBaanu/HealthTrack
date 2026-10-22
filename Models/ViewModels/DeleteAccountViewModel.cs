using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models.ViewModels
{
    public class DeleteAccountViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm with your password")]
        public string Password { get; set; } = string.Empty;
    }
}
