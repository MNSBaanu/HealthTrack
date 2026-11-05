using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services;

// Creates a user account and assigns its role in one transaction.
public static class AccountProvisioner
{
    // Creates the account and role together, rolling back if either step fails.
    public static async Task<IdentityResult> CreateAsync(HealthTrackDbContext context, UserManager<Person> users,
        Person person, string password, string role, string? specialty = null)
    {
        await using var operation = await AtomicOperation.BeginAsync(context);
        try
        {
            var result = await users.CreateAsync(person, password);
            if (!result.Succeeded) return result;
            result = await users.AddToRoleAsync(person, role);
            if (!result.Succeeded) return result;
            if (person is Trainer)
            {
                context.ConsultationProviders.Add(new ConsultationProvider
                {
                    Name = person.Name, PersonId = person.Id, IsActive = true,
                    Kind = ConsultationProviderKind.Trainer,
                    Specialty = string.IsNullOrWhiteSpace(specialty) ? "Fitness & Strength Coaching" : specialty.Trim()
                });
                await context.SaveChangesAsync();
            }
            await operation.CommitAsync();
            return IdentityResult.Success;
        }
        catch (DbUpdateException)
        {
            return IdentityResult.Failed(new IdentityError { Description = "The account could not be created. No changes were saved. Please try again." });
        }
    }
}
