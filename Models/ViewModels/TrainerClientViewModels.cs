namespace HealthTrack.Models.ViewModels;

public class TrainerClientSummaryViewModel
{
    public string ClientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FitnessGoal { get; set; }
    public double HeightCm { get; set; }
    public double? CurrentWeightKg { get; set; }
    public double? CurrentBmi { get; set; }
    public string? BmiCategory { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public int ActiveWorkoutPlansCount { get; set; }
    public int ActiveDietPlansCount { get; set; }
}

public class TrainerClientDetailsViewModel
{
    public Client Client { get; set; } = null!;
    public double? StartWeightKg { get; set; }
    public double? CurrentWeightKg { get; set; }
    public double? WeightChangeKg { get; set; }
    public double? CurrentBmi { get; set; }
    public string? BmiCategory { get; set; }

    public List<HealthMetric> MetricsHistory { get; set; } = new();
    public List<WorkoutLog> WorkoutHistory { get; set; } = new();
    public List<MealLog> MealHistory { get; set; } = new();

    public List<WorkoutPlan> WorkoutPlans { get; set; } = new();
    public List<DietPlan> DietPlans { get; set; } = new();
    public List<TrainerFeedback> Feedbacks { get; set; } = new();

    public TrainerFeedbackFormViewModel NewFeedbackForm { get; set; } = new();
}

public class ClientRecentActivityViewModel
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty; // "Workout", "Meal", "Metric"
    public string Summary { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
