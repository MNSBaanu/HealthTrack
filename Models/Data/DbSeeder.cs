using Microsoft.AspNetCore.Identity;

namespace HealthTrack.Models.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role in new[] { AppRoles.Admin, AppRoles.Client, AppRoles.Trainer })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var email = configuration["AdminAccount:Email"];
            var password = configuration["AdminAccount:Password"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            var userManager = services.GetRequiredService<UserManager<Person>>();
            if (await userManager.FindByEmailAsync(email) != null)
            {
                return;
            }

            var admin = new Admin
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Name = configuration["AdminAccount:Name"] ?? "Administrator",
                AccessLevel = 1
            };
            var result = await userManager.CreateAsync(admin, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, AppRoles.Admin);
            }
        }
    }
}
