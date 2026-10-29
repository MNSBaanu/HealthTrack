using HealthTrack.Models;

namespace HealthTrack.Services;

public static class TemporaryPasswordPolicy
{
    public static bool IsExpired(Person user, DateTime utcNow) => user.MustChangePassword
        && (!user.TemporaryPasswordExpiresAtUtc.HasValue || user.TemporaryPasswordExpiresAtUtc.Value <= utcNow);
}
