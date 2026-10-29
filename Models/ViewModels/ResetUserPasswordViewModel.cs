using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models.ViewModels;

public class ResetUserPasswordViewModel
{
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    [Display(Name = "New temporary password")]
    public string Password { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    [Display(Name = "Confirm temporary password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
