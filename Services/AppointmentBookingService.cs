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

    public async Task<string?> CancelAsync(string clientId, int appointmentId)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var appointment = await context.Appointments
                .SingleOrDefaultAsync(a => a.Id == appointmentId && a.ClientId == clientId);
            if (appointment == null) return "Appointment not found.";
            if (appointment.Status != AppointmentStatus.Pending)
                return "This appointment is no longer pending. Send a request to your trainer instead.";

            appointment.Status = AppointmentStatus.Cancelled;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return null;
        }
        catch (Exception exception) when (IsBookingConflict(exception))
        {
            context.ChangeTracker.Clear();
            return "Appointment state changed while processing. Please try again.";
        }
    }

    public async Task<string?> RescheduleAsync(string clientId, int appointmentId, int newSlotId)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var appointment = await context.Appointments.Include(a => a.Slot)
                .SingleOrDefaultAsync(a => a.Id == appointmentId && a.ClientId == clientId);
            if (appointment == null) return "Appointment not found.";
            if (appointment.Status != AppointmentStatus.Pending)
                return "This appointment is no longer pending. Send a request to your trainer instead.";

            var slot = await context.AvailabilitySlots.Include(s => s.Provider)
                .SingleOrDefaultAsync(s => s.Id == newSlotId && s.ProviderId == appointment.Slot.ProviderId);
            if (slot == null || !slot.IsAvailable || !slot.Provider.IsActive)
                return "That appointment time is no longer available. Choose another time.";
            if (slot.StartUtc <= clock.GetUtcNow().UtcDateTime || slot.EndUtc <= slot.StartUtc)
                return "Choose an appointment time in the future.";

            var conflicts = context.Appointments.Where(a => a.Id != appointmentId
                && a.Status != AppointmentStatus.Cancelled
                && a.Slot.StartUtc < slot.EndUtc && a.Slot.EndUtc > slot.StartUtc);
            if (await conflicts.AnyAsync(a => a.Slot.ProviderId == slot.ProviderId))
                return "The provider is already booked at that time. Choose another time.";
            if (await conflicts.AnyAsync(a => a.ClientId == clientId))
                return "You already have an appointment that overlaps this time.";

            appointment.SlotId = newSlotId;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return null;
        }
        catch (Exception exception) when (IsBookingConflict(exception))
        {
            context.ChangeTracker.Clear();
            return "Availability changed while you were rescheduling. Refresh and choose an available time.";
        }
    }

    private static bool IsBookingConflict(Exception exception) =>
        exception is SqlException { Number: 1205 or 2601 or 2627 }
        || exception is DbUpdateException { InnerException: SqlException { Number: 1205 or 2601 or 2627 } };

    public async Task<(bool Success, string? Error)> RescheduleClientAppointmentAsync(string clientId, int appointmentId, int newSlotId)
    {
        var appointment = await context.Appointments
            .Include(a => a.Slot).ThenInclude(s => s.Provider)
            .SingleOrDefaultAsync(a => a.Id == appointmentId && a.ClientId == clientId);

        if (appointment == null)
            return (false, "Appointment not found.");

        // Only allow rescheduling pending appointments
        if (appointment.Status != AppointmentStatus.Pending)
            return (false, "Only pending appointments can be rescheduled. Wait for trainer approval or contact them.");

        // Use serializable transaction to prevent concurrent conflicts
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var newSlot = await context.AvailabilitySlots.Include(s => s.Provider)
                .SingleOrDefaultAsync(s => s.Id == newSlotId);

            if (newSlot == null || !newSlot.IsAvailable || !newSlot.Provider.IsActive)
                return (false, "That appointment time is no longer available. Choose another time.");

            if (newSlot.StartUtc <= clock.GetUtcNow().UtcDateTime || newSlot.EndUtc <= newSlot.StartUtc)
                return (false, "Choose an appointment time in the future.");

            var conflicts = context.Appointments.Where(a => a.Status != AppointmentStatus.Cancelled
                && a.Slot.StartUtc < newSlot.EndUtc && a.Slot.EndUtc > newSlot.StartUtc);

            if (await conflicts.AnyAsync(a => a.Slot.ProviderId == newSlot.ProviderId && a.Id != appointmentId))
                return (false, "The provider is already booked at that time. Choose another time.");

            if (await conflicts.AnyAsync(a => a.ClientId == clientId && a.Id != appointmentId))
                return (false, "You already have an appointment that overlaps this time.");

            appointment.SlotId = newSlotId;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return (true, null);
        }
        catch (Exception exception) when (IsBookingConflict(exception))
        {
            context.ChangeTracker.Clear();
            return (false, "Availability changed while you were rescheduling. Refresh and try again.");
        }
    }

    public async Task<(bool Success, string? Error)> CancelClientAppointmentAsync(string clientId, int appointmentId)
    {
        var appointment = await context.Appointments
            .SingleOrDefaultAsync(a => a.Id == appointmentId && a.ClientId == clientId);

        if (appointment == null)
            return (false, "Appointment not found.");

        // Only allow cancelling pending appointments
        if (appointment.Status != AppointmentStatus.Pending)
            return (false, "Only pending appointments can be cancelled by you. Contact your trainer to cancel approved appointments.");

        appointment.Status = AppointmentStatus.Cancelled;
        await context.SaveChangesAsync();
        return (true, null);
    }
}
