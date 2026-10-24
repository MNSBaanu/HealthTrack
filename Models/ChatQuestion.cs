using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    public class ChatQuestion
    {
        public int Id { get; set; }

        [Required]
        [StringLength(300)]
        public string Question { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = string.Empty;

        public bool AiAnswered { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
