using System.Security.Claims;
using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HealthTrack.Services;

public interface IAuditService
{
    Task RecordAsync(ClaimsPrincipal user, string action, string entity, string? subjectClientId = null, string? detail = null);
}

public class AuditService(HealthTrackDbContext context) : IAuditService
{
    public async Task RecordAsync(ClaimsPrincipal user, string action, string entity, string? subjectClientId = null, string? detail = null)
    {
        context.AuditLogs.Add(AuditSaveChangesInterceptor.Build(user, action, entity, subjectClientId, detail));
        await context.SaveChangesAsync();
    }
}

public class AuditSaveChangesInterceptor(IHttpContextAccessor accessor) : SaveChangesInterceptor
{
    public static AuditLog Build(ClaimsPrincipal user, string action, string entity, string? subjectClientId, string? detail) => new()
    {
        ActorId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
        ActorName = user.Identity?.Name ?? string.Empty,
        ActorRole = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
        Action = action,
        Entity = entity,
        SubjectClientId = subjectClientId,
        Detail = detail is { Length: > 300 } ? detail[..300] : detail
    };

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Audit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Audit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Audit(DbContext? context)
    {
        var user = accessor.HttpContext?.User;
        if (context == null || user?.Identity?.IsAuthenticated != true) return;

        context.ChangeTracker.DetectChanges();
        var logs = new List<AuditLog>();
        foreach (var entry in context.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var subject = entry.Entity switch
            {
                HealthMetric m => m.ClientId,
                WorkoutLog w => w.ClientId,
                MealLog l => l.ClientId,
                _ => null
            };
            var adminRecord = user.IsInRole(AppRoles.Admin)
                && entry.Entity is Person or ClientTrainerAssignment or ConsultationProvider or AvailabilitySlot or AdminBroadcast;
            if (subject == null && !adminRecord) continue;
            if (entry.Entity is Client client) subject = client.Id;
            if (entry.Entity is ClientTrainerAssignment assignment) subject = assignment.ClientId;

            var action = entry.State switch { EntityState.Added => "Created", EntityState.Modified => "Updated", _ => "Deleted" };
            if (entry.Entity is Person && entry.State == EntityState.Modified)
            {
                var password = entry.Property(nameof(Person.PasswordHash));
                var lockout = entry.Property(nameof(Person.LockoutEnd));
                if (password.IsModified && !Equals(password.OriginalValue, password.CurrentValue)) action = "PasswordReset";
                else if (lockout.IsModified && !Equals(lockout.OriginalValue, lockout.CurrentValue))
                    action = entry.Property(nameof(Person.LockoutEnd)).CurrentValue == null ? "Unlocked" : "Locked";
            }
            var detail = entry.State == EntityState.Modified
                ? "Changed: " + string.Join(", ", entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name))
                : null;
            if (entry.Entity is Person person) detail = $"Account {person.Id}";
            else if (entry.Entity is ClientTrainerAssignment link) detail = $"Trainer {link.TrainerId}";
            else if (entry.Entity is AdminBroadcast broadcast) detail = $"{broadcast.RecipientDescription}; {broadcast.RecipientCount} recipients";
            logs.Add(Build(user, action, entry.Entity.GetType().Name, subject, detail));
        }
        if (logs.Count > 0) context.Set<AuditLog>().AddRange(logs);
    }
}
