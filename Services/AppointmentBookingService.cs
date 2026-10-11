using System.Data;
using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services;

public record BookingResult(int? AppointmentId, string? Error);

public class AppointmentBookingService(HealthTrackDbContext context, TimeProvider clock)
{
    public async Task<BookingResult> BookAsync(string clientId, int slotId, string? notes)
    {
        // Serialize the availability and conflict checks with the insert. This also
        // prevents simultaneous requests from booking overlapping provider slots.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var slot = await context.AvailabilitySlots.Include(s => s.Provider)
                .SingleOrDefaultAsync(s => s.Id == slotId);
            if (slot == null || !slot.IsAvailable || !slot.Provider.IsActive)
                return new(null, "That appointment time is no longer available. Choose another time.");
            if (slot.StartUtc <= clock.GetUtcNow().UtcDateTime || slot.EndUtc <= slot.StartUtc)
                return new(null, "Choose an appointment time in the future.");

            var conflicts = context.Appointments.Where(a => a.Status != AppointmentStatus.Cancelled
                && a.Slot.StartUtc < slot.EndUtc && a.Slot.EndUtc > slot.StartUtc);
            if (await conflicts.AnyAsync(a => a.Slot.ProviderId == slot.ProviderId))
                return new(null, "The provider is already booked at that time. Choose another time.");
            if (await conflicts.AnyAsync(a => a.ClientId == clientId))
                return new(null, "You already have an appointment that overlaps this time.");

            var appointment = new Appointment
            {
                ClientId = clientId, SlotId = slotId, Status = AppointmentStatus.Pending,
                RequestedAtUtc = clock.GetUtcNow().UtcDateTime,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            };
            context.Appointments.Add(appointment);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new(appointment.Id, null);
        }
        catch (Exception exception) when (IsBookingConflict(exception))
        {
            context.ChangeTracker.Clear();
            return new(null, "Availability changed while you were booking. Refresh and choose an available time.");
        }
    }

    private static bool IsBookingConflict(Exception exception) =>
        exception is SqlException { Number: 1205 or 2601 or 2627 }
        || exception is DbUpdateException { InnerException: SqlException { Number: 1205 or 2601 or 2627 } };
}
