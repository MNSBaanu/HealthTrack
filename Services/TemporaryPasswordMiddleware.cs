using HealthTrack.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace HealthTrack.Services;

public class TemporaryPasswordMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, UserManager<Person> users, TimeProvider clock)
    {
        if (context.User.Identity?.IsAuthenticated != true) { await next(context); return; }
        var user = await users.GetUserAsync(context.User);
        if (user?.MustChangePassword != true) { await next(context); return; }
        if (TemporaryPasswordPolicy.IsExpired(user, clock.GetUtcNow().UtcDateTime))
        {
            await context.SignOutAsync(IdentityConstants.ApplicationScheme);
            context.Response.Redirect("/Account/Login?temporaryPasswordExpired=true");
            return;
        }
        var path = context.Request.Path.Value?.TrimEnd('/');
        if (string.Equals(path, "/Account/ChangePassword", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "/Account/Logout", StringComparison.OrdinalIgnoreCase))
        { await next(context); return; }
        context.Response.Redirect("/Account/ChangePassword");
    }
}
