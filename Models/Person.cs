using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace HealthTrack.Models
{
    public abstract class Person : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastLoginAt { get; set; }

        public bool EmailNotificationsEnabled { get; set; } = true;

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        [NotMapped]
        public virtual string DashboardController => "Home";

        [NotMapped]
        public virtual string RoleDisplayName => "Member";

        [NotMapped]
        public string Initials => GetInitials(Name);

        public static string GetInitials(string name)
        {
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "?",
                1 => parts[0][..1].ToUpperInvariant(),
                _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
            };
        }
    }
}
