namespace HealthTrack.Models.ViewModels
{
    public class ProgressDataViewModel
    {
        public List<WeightPointViewModel> Weight { get; set; } = new();
        public List<DailyTotalViewModel> CaloriesBurned { get; set; } = new();
        public List<DailyTotalViewModel> CaloriesConsumed { get; set; } = new();
    }
}
