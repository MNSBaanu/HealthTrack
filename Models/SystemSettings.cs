using System.ComponentModel.DataAnnotations;
using HealthTrack.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Models
{
    public class SystemSettings
    {
        public int Id { get; set; } = 1;

        [Display(Name = "Show banner")]
        public bool BannerEnabled { get; set; }

        [StringLength(300)]
        [Display(Name = "Banner message")]
        public string? BannerMessage { get; set; }

        [Range(1, 336)]
        [Display(Name = "Temporary password expiry (hours)")]
        public int TemporaryPasswordHours { get; set; } = 48;

        [Range(1, 20)]
        [Display(Name = "Failed sign-ins before lockout")]
        public int MaxFailedAccessAttempts { get; set; } = 5;

        [Range(1, 1440)]
        [Display(Name = "Lockout length (minutes)")]
        public int LockoutMinutes { get; set; } = 10;

        [Display(Name = "Require two-factor sign-in for admins")]
        public bool RequireAdminTwoFactor { get; set; }

        [Range(1, 5000)]
        [Display(Name = "Max rows per import")]
        public int ImportMaxRows { get; set; } = 500;

        [Display(Name = "Email the weekly report to admins")]
        public bool WeeklyReportEnabled { get; set; }

        public DateTime? WeeklyReportSentAtUtc { get; set; }

        public void ApplyLockout(Microsoft.AspNetCore.Identity.IdentityOptions options)
        {
            options.Lockout.MaxFailedAccessAttempts = MaxFailedAccessAttempts;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(LockoutMinutes);
        }

        public static async Task<SystemSettings> LoadAsync(HealthTrackDbContext context, CancellationToken cancellationToken = default) =>
            await context.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? new SystemSettings();
    }
}
