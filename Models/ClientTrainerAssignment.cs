using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models;

public class ClientTrainerAssignment
{
    // The primary key guarantees one current Trainer per Client.
    public string ClientId { get; set; } = string.Empty;
    public Client Client { get; set; } = null!;
    public string TrainerId { get; set; } = string.Empty;
    public Trainer Trainer { get; set; } = null!;
    public DateTime AssignedAtUtc { get; set; }
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
