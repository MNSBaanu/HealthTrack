using System.Data;
using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services;

// Outcome of a booking attempt.
public record BookingResult(int? AppointmentId, string? Error);

// Books appointment slots safely so a slot cannot be double-booked.
public class AppointmentBookingService(HealthTrackDbContext context, TimeProvider clock,
    INotificationService notifications, AppointmentTime time)
{
    // Reserves the slot and creates a pending appointment in one transaction.
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

    public async Task<(bool Success, string? Error)> RescheduleClientAppointmentAsync(string clientId, int appointmentId, int newSlotId)
    {
        var appointment = await context.Appointments
            .Include(a => a.Slot).ThenInclude(s => s.Provider)
            .SingleOrDefaultAsync(a => a.Id == appointmentId && a.ClientId == clientId);

        if (appointment == null)
            return (false, "Appointment not found.");

        // Pending appointments can be moved freely; approved ones only with the trainer's permission.
        if (appointment.Status != AppointmentStatus.Pending && appointment.Status != AppointmentStatus.Approved)
            return (false, "This appointment can no longer be rescheduled.");

        // Use serializable transaction to prevent concurrent conflicts
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            RescheduleRequest? permission = null;
            if (appointment.Status == AppointmentStatus.Approved)
            {
                permission = await context.RescheduleRequests.SingleOrDefaultAsync(r =>
                    r.AppointmentId == appointmentId && r.Status == RescheduleRequestStatus.Accepted);
                if (permission == null)
                    return (false, "Your trainer must accept a reschedule request before you can change an approved appointment.");
                if (appointment.Slot.StartUtc <= clock.GetUtcNow().UtcDateTime)
                    return (false, "This appointment has already started and can no longer be rescheduled.");
            }

            var newSlot = await context.AvailabilitySlots.Include(s => s.Provider)
                .SingleOrDefaultAsync(s => s.Id == newSlotId);

            if (newSlot == null || !newSlot.IsAvailable || !newSlot.Provider.IsActive
                || newSlot.ProviderId != appointment.Slot.ProviderId)
                return (false, "That appointment time is no longer available. Choose another time.");

            if (newSlot.StartUtc <= clock.GetUtcNow().UtcDateTime || newSlot.EndUtc <= newSlot.StartUtc)
                return (false, "Choose an appointment time in the future.");

            var conflicts = context.Appointments.Where(a => a.Status != AppointmentStatus.Cancelled
                && a.Slot.StartUtc < newSlot.EndUtc && a.Slot.EndUtc > newSlot.StartUtc);

            if (await conflicts.AnyAsync(a => a.Slot.ProviderId == newSlot.ProviderId && a.Id != appointmentId))
                return (false, "The provider is already booked at that time. Choose another time.");

            if (await conflicts.AnyAsync(a => a.ClientId == clientId && a.Id != appointmentId))
                return (false, "You already have an appointment that overlaps this time.");

            var oldStart = time.ToLocal(appointment.Slot.StartUtc);
            appointment.SlotId = newSlotId;
            if (permission != null)
                permission.Status = RescheduleRequestStatus.Used;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            if (permission != null && appointment.Slot.Provider.PersonId is string trainerId)
            {
                var newStart = time.ToLocal(newSlot.StartUtc);
                var clientName = await context.Clients.Where(c => c.Id == clientId).Select(c => c.Name).SingleAsync();
                await notifications.SendAsync(trainerId,
                    $"{clientName} moved their consultation from {oldStart:MMM dd} at {oldStart:hh:mm tt} to {newStart:MMM dd, yyyy} at {newStart:hh:mm tt}.",
                    NotificationType.Appointment);
            }
            return (true, null);
        }
        catch (Exception exception) when (IsBookingConflict(exception))
        {
            context.ChangeTracker.Clear();
            return (false, "Availability changed while you were rescheduling. Refresh and try again.");
        }
    }

    public async Task<(bool Success, string? Error)> RequestRescheduleAsync(string clientId, int appointmentId, string reason)
    {
        var appointment = await context.Appointments
            .Include(a => a.Slot).ThenInclude(s => s.Provider)
            .Include(a => a.Client)
            .SingleOrDefaultAsync(a => a.Id == appointmentId && a.ClientId == clientId);

        if (appointment == null)
            return (false, "Appointment not found.");
        if (appointment.Status != AppointmentStatus.Approved)
            return (false, "Only approved appointments need a reschedule request.");
        if (appointment.Slot.StartUtc <= clock.GetUtcNow().UtcDateTime)
            return (false, "This appointment has already started and can no longer be rescheduled.");
        if (appointment.Slot.Provider.Kind != ConsultationProviderKind.Trainer || appointment.Slot.Provider.PersonId == null)
            return (false, "Reschedule requests can only be sent for appointments with a trainer.");

        var open = await context.RescheduleRequests.AnyAsync(r => r.AppointmentId == appointmentId
            && (r.Status == RescheduleRequestStatus.Pending || r.Status == RescheduleRequestStatus.Accepted));
        if (open)
            return (false, "You already have an open reschedule request for this appointment.");

        context.RescheduleRequests.Add(new RescheduleRequest
        {
            AppointmentId = appointmentId,
            Reason = reason.Trim(),
            Status = RescheduleRequestStatus.Pending,
            RequestedAtUtc = clock.GetUtcNow().UtcDateTime
        });
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsBookingConflict(exception))
        {
            // The filtered unique index rejected a second open request sent at the same moment.
            context.ChangeTracker.Clear();
            return (false, "You already have an open reschedule request for this appointment.");
        }

        var start = time.ToLocal(appointment.Slot.StartUtc);
        await notifications.SendAsync(appointment.Slot.Provider.PersonId,
            $"{appointment.Client.Name} asked to reschedule the consultation on {start:MMM dd, yyyy} at {start:hh:mm tt}. Review it on your Consultations page.",
            NotificationType.Appointment);
        return (true, null);
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
