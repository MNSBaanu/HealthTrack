namespace HealthTrack.Models.ViewModels
{
    public class ClientPerformanceViewModel
    {
        public string ClientId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public double HeightCm { get; set; }
        public double? StartWeightKg { get; set; }
        public double? CurrentWeightKg { get; set; }
        public int WorkoutCount { get; set; }
        public int TotalMinutes { get; set; }
        public int TotalCaloriesBurned { get; set; }
        public int MealLogCount { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public double? WeightChangeKg => StartWeightKg.HasValue && CurrentWeightKg.HasValue
            ? Math.Round(CurrentWeightKg.Value - StartWeightKg.Value, 1)
            : null;

        public double? CurrentBmi => CurrentWeightKg.HasValue
            ? HealthMetric.CalculateBmi(CurrentWeightKg.Value, HeightCm)
            : null;

        public string? BmiCategory => CurrentBmi.HasValue
            ? HealthMetric.GetBmiCategory(CurrentBmi.Value)
            : null;
    }
}
