namespace HealthTrack.Services
{
    public record Badge(string Icon, string Title, string Description, bool Earned);

    public record AchievementSummary(int CurrentStreak, int LongestStreak, List<Badge> Badges);

    public static class ClientAchievements
    {
        public static AchievementSummary Compute(IEnumerable<DateTime> workoutDates, DateTime today, int totalMeals, int totalWeighIns, bool goalReached)
        {
            var days = workoutDates.Select(d => d.Date).Distinct().OrderBy(d => d).ToList();
            var totalWorkoutDays = days.Count;

            var longest = 0;
            var run = 0;
            DateTime? previous = null;
            foreach (var day in days)
            {
                run = previous.HasValue && (day - previous.Value).Days == 1 ? run + 1 : 1;
                longest = Math.Max(longest, run);
                previous = day;
            }

            // A streak is still alive if the last workout was today or yesterday.
            var current = 0;
            if (previous.HasValue && (today.Date - previous.Value).Days <= 1)
            {
                var cursor = previous.Value;
                var set = days.ToHashSet();
                while (set.Contains(cursor))
                {
                    current++;
                    cursor = cursor.AddDays(-1);
                }
            }

            var badges = new List<Badge>
            {
                new("icon-zap", "First workout", "Log your first workout", totalWorkoutDays >= 1),
                new("icon-flame", "3-day streak", "Work out 3 days in a row", longest >= 3),
                new("icon-flame", "7-day streak", "Work out 7 days in a row", longest >= 7),
                new("icon-trophy", "10 workout days", "Train on 10 different days", totalWorkoutDays >= 10),
                new("icon-coffee", "Meal tracker", "Log 10 meals", totalMeals >= 10),
                new("icon-ruler", "Regular weigh-ins", "Record 5 weigh-ins", totalWeighIns >= 5),
                new("icon-target", "Goal reached", "Reach your target weight", goalReached)
            };
            return new AchievementSummary(current, longest, badges);
        }
    }
}
