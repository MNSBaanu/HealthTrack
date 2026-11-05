using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models;

// Feedback a trainer sent to a client, with the client's reply.
public class TrainerFeedback
{
    public int Id { get; set; }

    [Required]
    public string TrainerId { get; set; } = string.Empty;
    public Trainer? Trainer { get; set; }

    [Required]
    public string ClientId { get; set; } = string.Empty;
    public Client? Client { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Comments { get; set; } = string.Empty;

    public FeedbackCategory Category { get; set; } = FeedbackCategory.General;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsReadByClient { get; set; }

    [StringLength(1000)]
    public string? ClientReply { get; set; }

    public DateTime? ClientRepliedAtUtc { get; set; }
}
