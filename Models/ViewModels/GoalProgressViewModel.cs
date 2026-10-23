namespace HealthTrack.Models.ViewModels
{
    public class GoalProgressViewModel
    {
        public double StartWeightKg { get; set; }
        public double CurrentWeightKg { get; set; }
        public double TargetWeightKg { get; set; }
        public DateTime? TargetDate { get; set; }
        public double RemainingKg { get; set; }
        public int PercentComplete { get; set; }
        public bool Reached { get; set; }
        public DateTime? ProjectedDate { get; set; }
        public bool? OnTrack { get; set; }

        public static GoalProgressViewModel? Create(Client client, IReadOnlyList<HealthMetric> metrics)
        {
            if (client.TargetWeightKg is not double target || metrics.Count == 0)
            {
                return null;
            }

            var first = metrics[0];
            var last = metrics[^1];
            var start = first.WeightKg;
            var current = last.WeightKg;
            var goal = new GoalProgressViewModel
            {
                StartWeightKg = start,
                CurrentWeightKg = current,
                TargetWeightKg = target,
                TargetDate = client.TargetDate,
                RemainingKg = Math.Round(Math.Abs(current - target), 1)
            };

            goal.Reached = start >= target ? current <= target : current >= target;
            var totalChange = Math.Abs(start - target);
            var done = start >= target ? start - current : current - start;
            goal.PercentComplete = goal.Reached ? 100 : totalChange < 0.05 ? 0 : (int)Math.Clamp(done / totalChange * 100, 0, 100);

            var days = (last.RecordedAt.Date - first.RecordedAt.Date).TotalDays;
            var perDay = days >= 1 ? (current - start) / days : 0;
            var towardTarget = target < current ? perDay < -0.005 : perDay > 0.005;
            if (!goal.Reached && towardTarget)
            {
                goal.ProjectedDate = DateTime.Today.AddDays(Math.Ceiling(Math.Abs(current - target) / Math.Abs(perDay)));
                goal.OnTrack = client.TargetDate.HasValue ? goal.ProjectedDate <= client.TargetDate.Value.Date : null;
            }
            else if (!goal.Reached && client.TargetDate.HasValue)
            {
                goal.OnTrack = false;
            }
            return goal;
        }
    }
}
