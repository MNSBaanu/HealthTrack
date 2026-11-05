using HealthTrack.Models;

namespace HealthTrack.Tests;

// Checks the BMI calculation and the category shown to clients.
public class HealthMetricTests
{
    [Theory]
    [InlineData(70, 175, 22.9)]
    [InlineData(90, 180, 27.8)]
    [InlineData(50, 160, 19.5)]
    public void CalculateBmi_ReturnsRoundedValue(double weightKg, double heightCm, double expected)
    {
        Assert.Equal(expected, HealthMetric.CalculateBmi(weightKg, heightCm));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void CalculateBmi_ReturnsNull_WhenHeightIsNotPositive(double heightCm)
    {
        Assert.Null(HealthMetric.CalculateBmi(70, heightCm));
    }

    [Theory]
    [InlineData(null, "Unknown")]
    [InlineData(17.0, "Underweight")]
    [InlineData(18.5, "Normal")]
    [InlineData(24.9, "Normal")]
    [InlineData(25.0, "Overweight")]
    [InlineData(30.0, "Obese")]
    public void GetBmiCategory_UsesStandardRanges(double? bmi, string expected)
    {
        Assert.Equal(expected, HealthMetric.GetBmiCategory(bmi));
    }
}
