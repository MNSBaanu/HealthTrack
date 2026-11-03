using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models.ViewModels
{
    public class LoginWith2faViewModel
    {
        [Required]
        [StringLength(20)]
        [Display(Name = "Authentication code")]
        public string Code { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public class EnableAuthenticatorViewModel
    {
        [Required]
        [StringLength(7, MinimumLength = 6)]
        [Display(Name = "Verification code")]
        public string Code { get; set; } = string.Empty;

        public string SharedKey { get; set; } = string.Empty;

        public string QrCodeDataUri { get; set; } = string.Empty;
    }
}
