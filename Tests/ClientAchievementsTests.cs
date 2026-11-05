using HealthTrack.Services;

namespace HealthTrack.Tests;

// Checks workout streaks and badge rules on the client dashboard.
public class ClientAchievementsTests
{
    private static readonly DateTime Today = new(2026, 11, 5);

    private static List<DateTime> Days(params int[] daysAgo) => daysAgo.Select(d => Today.AddDays(-d)).ToList();

    [Fact]
    public void Compute_CountsCurrentAndLongestStreak()
    {
        var result = ClientAchievements.Compute(Days(0, 1, 2, 5, 6, 7, 8), Today, 0, 0, false);
        Assert.Equal(3, result.CurrentStreak);
        Assert.Equal(4, result.LongestStreak);
    }

    [Fact]
    public void Compute_StreakStaysAlive_WhenLastWorkoutWasYesterday()
    {
        Assert.Equal(2, ClientAchievements.Compute(Days(1, 2), Today, 0, 0, false).CurrentStreak);
    }

    [Fact]
    public void Compute_StreakResets_AfterAMissedDay()
    {
        Assert.Equal(0, ClientAchievements.Compute(Days(2, 3), Today, 0, 0, false).CurrentStreak);
    }

    [Fact]
    public void Compute_CountsSeveralWorkoutsOnOneDayOnce()
    {
        var result = ClientAchievements.Compute(new[] { Today.AddHours(8), Today.AddHours(18) }, Today, 0, 0, false);
        Assert.Equal(1, result.LongestStreak);
    }

    [Fact]
    public void Compute_EarnsBadgesFromTotals()
    {
        var badges = ClientAchievements.Compute(Days(0), Today, 10, 5, true).Badges.ToDictionary(b => b.Title, b => b.Earned);
        Assert.True(badges["First workout"]);
        Assert.True(badges["Meal tracker"]);
        Assert.True(badges["Regular weigh-ins"]);
        Assert.True(badges["Goal reached"]);
        Assert.False(badges["3-day streak"]);
    }
}
