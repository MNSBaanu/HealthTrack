using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    public class Admin : Person
    {
        [Range(1, 3)]
        public int AccessLevel { get; set; } = 1;
    }
}
