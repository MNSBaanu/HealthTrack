namespace HealthTrack.Models.ViewModels
{
    public class PlanExerciseStatViewModel
    {
        public string Exercise { get; set; } = string.Empty;
        public int PlanCount { get; set; }
        public int ClientCount { get; set; }
        public int Completions { get; set; }
    }

    public class PlanStatViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Trainer { get; set; } = string.Empty;
        public string Client { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public int Completions { get; set; }
    }
}
