using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    public class Admin : Person
    {
        [Range(1, 3)]
        public int AccessLevel { get; set; } = 1;

        [NotMapped]
        public override string DashboardController => "Admin";

        [NotMapped]
        public override string RoleDisplayName => "Administrator";
    }
}
