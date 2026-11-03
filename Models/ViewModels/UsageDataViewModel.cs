namespace HealthTrack.Models.ViewModels
{
    public class UsageDataViewModel
    {
        public List<string> Labels { get; set; } = new();
        public List<int> Workouts { get; set; } = new();
        public List<int> Meals { get; set; } = new();
        public List<int> Metrics { get; set; } = new();
        public List<int> Registrations { get; set; } = new();
        public List<int> Appointments { get; set; } = new();
        public List<int> Plans { get; set; } = new();
        public List<int> Feedback { get; set; } = new();
    }
}
