using HealthTrack.Models;
using HealthTrack.Services;

namespace HealthTrack.Tests;

// Checks when an admin-issued temporary password stops working.
public class TemporaryPasswordPolicyTests
{
    private static readonly DateTime Now = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void IsExpired_False_WhenNoPasswordChangeIsRequired()
    {
        Assert.False(TemporaryPasswordPolicy.IsExpired(new Client { MustChangePassword = false }, Now));
    }

    [Fact]
    public void IsExpired_False_BeforeExpiryTime()
    {
        var user = new Client { MustChangePassword = true, TemporaryPasswordExpiresAtUtc = Now.AddHours(1) };
        Assert.False(TemporaryPasswordPolicy.IsExpired(user, Now));
    }

    [Fact]
    public void IsExpired_True_AtOrAfterExpiryTime()
    {
        var user = new Client { MustChangePassword = true, TemporaryPasswordExpiresAtUtc = Now };
        Assert.True(TemporaryPasswordPolicy.IsExpired(user, Now));
    }

    [Fact]
    public void IsExpired_True_WhenExpiryIsMissing()
    {
        Assert.True(TemporaryPasswordPolicy.IsExpired(new Client { MustChangePassword = true }, Now));
    }
}
