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

            var trainerEmail = "trainer@healthtrack.com";
            if (await userManager.FindByEmailAsync(trainerEmail) == null)
            {
                var trainer = new Trainer
                {
                    UserName = trainerEmail,
                    Email = trainerEmail,
                    EmailConfirmed = true,
                    Name = "Coach Alex Rivera",
                    DateOfBirth = new DateTime(1992, 5, 14)
                };
                var trainerResult = await userManager.CreateAsync(trainer, "Trainer@12345");
                if (trainerResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(trainer, AppRoles.Trainer);

                    var dbContext = services.GetRequiredService<HealthTrackDbContext>();
                    dbContext.ConsultationProviders.Add(new ConsultationProvider
                    {
                        Name = trainer.Name,
                        Specialty = "Strength, Conditioning & Hypertrophy",
                        Kind = ConsultationProviderKind.Trainer,
                        IsActive = true,
                        PersonId = trainer.Id
                    });
                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }
}
