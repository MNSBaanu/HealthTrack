namespace HealthTrack.Models.ViewModels
{
    public class ActivityStatViewModel
    {
        public string Activity { get; set; } = string.Empty;
        public int Sessions { get; set; }
        public int ClientCount { get; set; }
        public int TotalMinutes { get; set; }
    }
}
