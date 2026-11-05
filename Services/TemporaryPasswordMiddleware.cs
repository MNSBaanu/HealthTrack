using HealthTrack.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace HealthTrack.Services;

// Sends users with a temporary password to the change password page until they set their own.
public class TemporaryPasswordMiddleware(RequestDelegate next)
{
    // Checks the signed-in user and redirects when a password change is required.
    public async Task InvokeAsync(HttpContext context, UserManager<Person> users, TimeProvider clock, HealthTrack.Models.Data.HealthTrackDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated != true) { await next(context); return; }
        var user = await users.GetUserAsync(context.User);
        if (user == null) { await next(context); return; }
        if (!user.MustChangePassword)
        {
            if (user is Admin && !user.TwoFactorEnabled && !IsPath(context, "/Account/EnableAuthenticator", "/Account/Logout")
                && (await SystemSettings.LoadAsync(db)).RequireAdminTwoFactor)
            {
                context.Response.Redirect("/Account/EnableAuthenticator");
                return;
            }
            await next(context);
            return;
        }
        if (TemporaryPasswordPolicy.IsExpired(user, clock.GetUtcNow().UtcDateTime))
        {
            await context.SignOutAsync(IdentityConstants.ApplicationScheme);
            context.Response.Redirect("/Account/Login?temporaryPasswordExpired=true");
            return;
        }
        if (IsPath(context, "/Account/ChangePassword", "/Account/Logout")) { await next(context); return; }
        context.Response.Redirect("/Account/ChangePassword");
    }

    private static bool IsPath(HttpContext context, params string[] paths)
    {
        var path = context.Request.Path.Value?.TrimEnd('/');
        return paths.Any(p => string.Equals(path, p, StringComparison.OrdinalIgnoreCase));
    }
}
