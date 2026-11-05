using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    // A health measurement recorded by a client.
    public class HealthMetric
    {
        [Key]
        public int MetricId { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;

        public Client? Client { get; set; }

        [Range(1, 500)]
        public double WeightKg { get; set; }

        [Range(1, 75)]
        public double? BodyFatPercent { get; set; }

        [Range(20, 250)]
        public int? RestingHeartRate { get; set; }

        [Range(50, 260)]
        public int? SystolicBp { get; set; }

        [Range(30, 160)]
        public int? DiastolicBp { get; set; }

        [Range(0, 100000)]
        public int? Steps { get; set; }

        [Range(0, 24)]
        public double? SleepHours { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public double? Bmi => Client == null ? null : CalculateBmi(WeightKg, Client.HeightCm);

        [NotMapped]
        public string BmiCategory => GetBmiCategory(Bmi);

        // Calculates BMI from weight in kg and height in cm.
        public static double? CalculateBmi(double weightKg, double heightCm)
        {
            if (heightCm <= 0)
            {
                return null;
            }
            var heightM = heightCm / 100;
            return Math.Round(weightKg / (heightM * heightM), 1);
        }

        // Returns the weight category for a BMI value.
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
