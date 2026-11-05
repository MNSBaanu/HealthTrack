using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    // A question and answer used by the chatbot.
    public class ChatFaq
    {
        public int Id { get; set; }

        [Required]
        [StringLength(300)]
        public string Question { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Answer { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
