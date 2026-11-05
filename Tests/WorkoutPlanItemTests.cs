using HealthTrack.Models;

namespace HealthTrack.Tests;

// Checks when a planned exercise can be ticked off by the client.
public class WorkoutPlanItemTests
{
    private static readonly DateTime Monday = new(2026, 11, 2);

    private static WorkoutPlanItem Item(bool active = true, DateTime? start = null, DateTime? end = null, DayOfWeek? day = null) => new()
    {
        DayOfWeek = day,
        WorkoutPlan = new WorkoutPlan { IsActive = active, StartDate = start ?? Monday, EndDate = end }
    };

    [Fact]
    public void CanCompleteOn_ActivePlanWithinDates_ReturnsTrue()
    {
        Assert.True(Item(end: Monday.AddDays(6)).CanCompleteOn(Monday.AddDays(3)));
    }

    [Fact]
    public void CanCompleteOn_PausedPlan_ReturnsFalse()
    {
        Assert.False(Item(active: false).CanCompleteOn(Monday));
    }

    [Fact]
    public void CanCompleteOn_BeforeStartOrAfterEnd_ReturnsFalse()
    {
        var item = Item(end: Monday.AddDays(6));
        Assert.False(item.CanCompleteOn(Monday.AddDays(-1)));
        Assert.False(item.CanCompleteOn(Monday.AddDays(7)));
    }

    [Fact]
    public void CanCompleteOn_OnlyOnScheduledWeekday()
    {
        var item = Item(day: DayOfWeek.Wednesday);
        Assert.True(item.CanCompleteOn(Monday.AddDays(2)));
        Assert.False(item.CanCompleteOn(Monday.AddDays(1)));
    }

    [Fact]
    public void CanCompleteOn_WithoutPlan_ReturnsFalse()
    {
        Assert.False(new WorkoutPlanItem().CanCompleteOn(Monday));
    }
}
