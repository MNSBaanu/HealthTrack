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

        public DateTime RecordedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public double? Bmi => Client == null ? null : CalculateBmi(WeightKg, Client.HeightCm);

        [NotMapped]
        public string BmiCategory => GetBmiCategory(Bmi);

        public static double? CalculateBmi(double weightKg, double heightCm)
        {
            if (heightCm <= 0)
            {
                return null;
            }
            var heightM = heightCm / 100;
            return Math.Round(weightKg / (heightM * heightM), 1);
        }

        public static string GetBmiCategory(double? bmi) => bmi switch
        {
            null => "Unknown",
            < 18.5 => "Underweight",
            < 25 => "Normal",
            < 30 => "Overweight",
            _ => "Obese"
        };
    }
}
