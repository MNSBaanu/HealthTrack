using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models;

public class WeeklyCheckIn
{
    public int Id { get; set; }

    [Required]
    public string TrainerId { get; set; } = string.Empty;
    public Trainer? Trainer { get; set; }

    [Required]
    public string ClientId { get; set; } = string.Empty;
    public Client? Client { get; set; }

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;

    [StringLength(300)]
    public string? Note { get; set; }

    public DateTime? RespondedAtUtc { get; set; }

    [Range(1, 5)]
    public int? Energy { get; set; }

    [Range(1, 5)]
    public int? Mood { get; set; }

    [Range(0, 10)]
    public int? Pain { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    public bool IsAnswered => RespondedAtUtc.HasValue;
}
