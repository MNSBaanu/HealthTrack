using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models;

public class TrainerAssignmentHistory
{
    public int Id { get; set; }

    [Required]
    public string ClientId { get; set; } = string.Empty;
    public Client? Client { get; set; }

    [Required]
    public string TrainerId { get; set; } = string.Empty;
    public Trainer? Trainer { get; set; }

    public DateTime AssignedAtUtc { get; set; }

    public DateTime? EndedAtUtc { get; set; }
}
