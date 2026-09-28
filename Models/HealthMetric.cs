using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    public class HealthMetric
    {
        [Key]
        public int MetricId { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;

        public Client? Client { get; set; }

        [Range(1, 500)]
        public double WeightKg { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public double? Bmi => Client is { HeightCm: > 0 }
            ? Math.Round(WeightKg / Math.Pow(Client.HeightCm / 100, 2), 1)
            : null;
    }
}
