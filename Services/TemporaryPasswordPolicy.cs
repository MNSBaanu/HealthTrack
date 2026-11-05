using HealthTrack.Models;

namespace HealthTrack.Services;

// Rules for when a temporary password expires.
public static class TemporaryPasswordPolicy
{
    // Returns true when the temporary password is too old to use.
    public static bool IsExpired(Person user, DateTime utcNow) => user.MustChangePassword
        && (!user.TemporaryPasswordExpiresAtUtc.HasValue || user.TemporaryPasswordExpiresAtUtc.Value <= utcNow);
}
