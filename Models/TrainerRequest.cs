using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models;

public enum TrainerRequestStatus
{
    Pending = 0,
    Fulfilled = 1,
    Declined = 2,
    Cancelled = 3
}

// A client's request for a trainer, or for a different one, which an administrator answers.
public class TrainerRequest
{
    public int Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public Client Client { get; set; } = null!;
    public string? PreferredTrainerId { get; set; }
    public Trainer? PreferredTrainer { get; set; }
    // True when the client already had a trainer at the time of the request.
    public bool IsChange { get; set; }
    [StringLength(300)]
    public string? Note { get; set; }
    public TrainerRequestStatus Status { get; set; } = TrainerRequestStatus.Pending;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    [StringLength(300)]
    public string? AdminResponse { get; set; }
}
