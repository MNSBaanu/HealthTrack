using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace HealthTrack.Services;

// Loads and prepares trainer schedule data.
public class TrainerScheduleService : ITrainerScheduleService
{
    private readonly HealthTrackDbContext _context;
    private readonly AppointmentTime _time;
    private readonly INotificationService _notificationService;
    private readonly TimeProvider _clock;

    public TrainerScheduleService(
        HealthTrackDbContext context,
        AppointmentTime time,
        INotificationService notificationService,
        TimeProvider clock)
    {
        _context = context;
        _time = time;
        _notificationService = notificationService;
        _clock = clock;
    }

    // Returns the trainer's consultation provider record, creating it on first use.
    public async Task<ConsultationProvider> GetOrCreateTrainerProviderAsync(string trainerId)
    {
        var provider = await _context.ConsultationProviders
            .Include(p => p.Person)
            .FirstOrDefaultAsync(p => p.PersonId == trainerId);

        if (provider != null) return provider;

        var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.Id == trainerId);
        if (trainer == null)
        {
            throw new InvalidOperationException($"Trainer with ID {trainerId} not found in the database.");
        }

        provider = new ConsultationProvider
        {
            PersonId = trainer.Id,
            Name = trainer.Name,
            Specialty = "Fitness & Strength Coaching",
            Kind = ConsultationProviderKind.Trainer,
            IsActive = true
        };

        _context.ConsultationProviders.Add(provider);
        await _context.SaveChangesAsync();
        return provider;
    }

    // Loads the trainer's slots, appointments and reschedule requests.
    public async Task<TrainerScheduleViewModel> GetScheduleAsync(string trainerId)
    {
        var provider = await GetOrCreateTrainerProviderAsync(trainerId);
        var nowUtc = _clock.GetUtcNow().UtcDateTime;

        var slots = await _context.AvailabilitySlots
            .Where(s => s.ProviderId == provider.Id && s.IsAvailable && s.EndUtc >= nowUtc)
            .OrderBy(s => s.StartUtc)
            .Select(s => new TrainerAvailabilitySlotViewModel
            {
                Id = s.Id,
                StartLocal = _time.ToLocal(s.StartUtc),
                EndLocal = _time.ToLocal(s.EndUtc),
                IsBooked = s.Appointments.Any(a => a.Status != AppointmentStatus.Cancelled)
            })
            .ToListAsync();

        var appointments = await _context.Appointments
            .Include(a => a.Client)
            .Include(a => a.Slot)
            .Where(a => a.Slot.ProviderId == provider.Id)
            .OrderBy(a => a.Slot.StartUtc)
            .ToListAsync();

        var openRequests = await _context.RescheduleRequests.AsNoTracking()
            .Where(r => r.Appointment.Slot.ProviderId == provider.Id
                && (r.Status == RescheduleRequestStatus.Pending || r.Status == RescheduleRequestStatus.Accepted))
            .Select(r => new { r.Id, r.AppointmentId, r.Status, r.Reason, r.RequestedAtUtc })
            .ToListAsync();

        var pending = new List<TrainerAppointmentItemViewModel>();
        var approved = new List<TrainerAppointmentItemViewModel>();
        var pastOrCancelled = new List<TrainerAppointmentItemViewModel>();
        var rescheduleRequests = new List<TrainerRescheduleRequestViewModel>();

        foreach (var appt in appointments)
        {
            var request = openRequests.FirstOrDefault(r => r.AppointmentId == appt.Id);
            var isUpcomingApproved = appt.Status == AppointmentStatus.Approved && appt.Slot.StartUtc > nowUtc;
            if (request?.Status == RescheduleRequestStatus.Pending && isUpcomingApproved)
            {
                rescheduleRequests.Add(new TrainerRescheduleRequestViewModel
                {
                    RequestId = request.Id,
                    AppointmentId = appt.Id,
                    ClientName = appt.Client?.Name ?? "Client",
                    ClientEmail = appt.Client?.Email ?? string.Empty,
                    StartLocal = _time.ToLocal(appt.Slot.StartUtc),
                    EndLocal = _time.ToLocal(appt.Slot.EndUtc),
                    Reason = request.Reason,
                    RequestedAtLocal = _time.ToLocal(request.RequestedAtUtc)
                });
            }

            var item = new TrainerAppointmentItemViewModel
            {
                Id = appt.Id,
                ClientId = appt.ClientId,
                ClientName = appt.Client?.Name ?? "Client",
                ClientEmail = appt.Client?.Email ?? string.Empty,
                StartLocal = _time.ToLocal(appt.Slot.StartUtc),
                EndLocal = _time.ToLocal(appt.Slot.EndUtc),
                Status = appt.Status,
                Notes = appt.Notes,
                RequestedAtLocal = _time.ToLocal(appt.RequestedAtUtc),
                ClientMayReschedule = request?.Status == RescheduleRequestStatus.Accepted && isUpcomingApproved
            };

            if (appt.Status == AppointmentStatus.Pending)
            {
                pending.Add(item);
            }
            else if (appt.Status == AppointmentStatus.Approved && appt.Slot.EndUtc >= nowUtc)
            {
                approved.Add(item);
            }
            else
            {
                pastOrCancelled.Add(item);
            }
        }

        return new TrainerScheduleViewModel
        {
            ProviderName = provider.Name,
            Specialty = provider.Specialty,
            TimeZoneLabel = _time.Label,
            NewSlotForm = new AddAvailabilitySlotForm
            {
                StartLocal = DateTime.Today.AddDays(1).AddHours(9),
                EndLocal = DateTime.Today.AddDays(1).AddHours(10)
            },
            UpcomingSlots = slots,
            PendingAppointments = pending,
            RescheduleRequests = rescheduleRequests,
            UpcomingApprovedAppointments = approved,
            PastOrCancelledAppointments = pastOrCancelled
        };
    }

    public async Task<(bool Success, string? Error)> AddAvailabilitySlotAsync(string trainerId, DateTime startLocal, DateTime endLocal)
    {
        if (endLocal <= startLocal)
        {
            return (false, "Slot end time must be after start time.");
        }

        if (endLocal - startLocal < TimeSpan.FromMinutes(15) || endLocal - startLocal > TimeSpan.FromHours(8))
        {
            return (false, "A slot must last between 15 minutes and 8 hours.");
        }

        var startUtc = _time.ToUtc(startLocal);
        var endUtc = _time.ToUtc(endLocal);
        var nowUtc = _clock.GetUtcNow().UtcDateTime;

        if (startUtc <= nowUtc)
        {
            return (false, "Slot start time must be in the future.");
        }

        var provider = await GetOrCreateTrainerProviderAsync(trainerId);

        // Check for overlapping slots
        var hasOverlap = await _context.AvailabilitySlots.AnyAsync(s =>
            s.ProviderId == provider.Id &&
            s.IsAvailable &&
            s.StartUtc < endUtc &&
            s.EndUtc > startUtc);

        if (hasOverlap)
        {
            return (false, "You already have an availability slot that overlaps with this time range.");
        }

        var slot = new AvailabilitySlot
        {
            ProviderId = provider.Id,
            StartUtc = startUtc,
            EndUtc = endUtc,
            IsAvailable = true
        };

        _context.AvailabilitySlots.Add(slot);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteSlotAsync(string trainerId, int slotId)
    {
        var provider = await GetOrCreateTrainerProviderAsync(trainerId);
        var slot = await _context.AvailabilitySlots
            .Include(s => s.Appointments)
            .FirstOrDefaultAsync(s => s.Id == slotId && s.ProviderId == provider.Id && s.IsAvailable);

        if (slot == null) return (false, "Slot not found.");

        if (slot.Appointments.Any(a => a.Status != AppointmentStatus.Cancelled))
        {
            return (false, "Cannot delete a slot that has an active booking. Cancel the appointment first.");
        }

        // Slots with cancelled bookings are hidden instead of deleted so the appointment history stays intact.
        if (slot.Appointments.Any()) slot.IsAvailable = false;
        else _context.AvailabilitySlots.Remove(slot);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    // Loads a slot the trainer owns for editing.
    public async Task<AvailabilitySlot?> GetSlotForEditAsync(string trainerId, int slotId)
    {
        var provider = await GetOrCreateTrainerProviderAsync(trainerId);
        return await _context.AvailabilitySlots
            .FirstOrDefaultAsync(s => s.Id == slotId && s.ProviderId == provider.Id && s.IsAvailable);
    }

    public async Task<(bool Success, string? Error)> UpdateSlotAsync(string trainerId, int slotId, DateTime startLocal, DateTime endLocal)
    {
        if (endLocal <= startLocal)
        {
            return (false, "Slot end time must be after start time.");
        }

        if (endLocal - startLocal < TimeSpan.FromMinutes(15) || endLocal - startLocal > TimeSpan.FromHours(8))
        {
            return (false, "A slot must last between 15 minutes and 8 hours.");
        }

        var startUtc = _time.ToUtc(startLocal);
        var endUtc = _time.ToUtc(endLocal);
        var nowUtc = _clock.GetUtcNow().UtcDateTime;

        if (startUtc <= nowUtc)
        {
            return (false, "Slot start time must be in the future.");
        }

        var provider = await GetOrCreateTrainerProviderAsync(trainerId);
        var slot = await _context.AvailabilitySlots
            .Include(s => s.Appointments)
            .FirstOrDefaultAsync(s => s.Id == slotId && s.ProviderId == provider.Id && s.IsAvailable);

        if (slot == null) return (false, "Slot not found.");

        // If slot is booked, don't allow editing the time
        if (slot.Appointments.Any(a => a.Status != AppointmentStatus.Cancelled))
        {
            return (false, "Cannot edit a slot that has an active booking. Delete and create a new slot instead.");
        }

        // Check for overlapping slots with other existing slots
        var hasOverlap = await _context.AvailabilitySlots.AnyAsync(s =>
            s.ProviderId == provider.Id &&
            s.IsAvailable &&
            s.Id != slotId &&
            s.StartUtc < endUtc &&
            s.EndUtc > startUtc);

        if (hasOverlap)
        {
            return (false, "You already have an availability slot that overlaps with this time range.");
        }

        slot.StartUtc = startUtc;
        slot.EndUtc = endUtc;
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ApproveAppointmentAsync(string trainerId, int appointmentId)
    {
        var provider = await GetOrCreateTrainerProviderAsync(trainerId);

        // Use serializable transaction to prevent concurrent approval requests
        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var appointment = await _context.Appointments
                .Include(a => a.Slot)
                .Include(a => a.Client)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.Slot.ProviderId == provider.Id);

            if (appointment == null) return (false, "Appointment not found.");

            // Idempotency check: if already approved, return success without duplicate notification
            if (appointment.Status == AppointmentStatus.Approved)
            {
                await transaction.RollbackAsync();
                return (true, null);
            }

            // Reject if in any other non-pending state
            if (appointment.Status != AppointmentStatus.Pending)
            {
                return (false, $"Cannot approve appointment that is already {appointment.Status}.");
            }

            if (appointment.Slot.StartUtc <= _clock.GetUtcNow().UtcDateTime)
            {
                return (false, "This appointment time has already passed.");
            }

            appointment.Status = AppointmentStatus.Approved;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var localStart = _time.ToLocal(appointment.Slot.StartUtc);
            await _notificationService.SendAsync(
                appointment.ClientId,
                $"Your consultation appointment on {localStart:MMM dd, yyyy} at {localStart:hh:mm tt} was APPROVED by your Trainer.",
                NotificationType.Appointment);

            return (true, null);
        }
        catch (Exception exception) when (IsApprovalConflict(exception))
        {
            _context.ChangeTracker.Clear();
            return (false, "Appointment state changed while processing. Please try again.");
        }
    }

    public async Task<(bool Success, string? Error)> CancelAppointmentAsync(string trainerId, int appointmentId, string? reason)
    {
        var provider = await GetOrCreateTrainerProviderAsync(trainerId);

        // Use serializable transaction to prevent concurrent cancellation requests
        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var appointment = await _context.Appointments
                .Include(a => a.Slot)
                .Include(a => a.Client)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.Slot.ProviderId == provider.Id);

            if (appointment == null) return (false, "Appointment not found.");

            // Idempotency check: if already cancelled, return success without duplicate notification
            if (appointment.Status == AppointmentStatus.Cancelled)
            {
                await transaction.RollbackAsync();
                return (true, null);
            }

            // Reject if already completed
            if (appointment.Status == AppointmentStatus.Completed)
            {
                return (false, "Cannot cancel an appointment that is already completed.");
            }

            var oldStatus = appointment.Status;
            appointment.Status = AppointmentStatus.Cancelled;
            await CloseOpenRescheduleRequestsAsync(appointment.Id);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var localStart = _time.ToLocal(appointment.Slot.StartUtc);
            var note = string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason.Trim()}";
            await _notificationService.SendAsync(
                appointment.ClientId,
                $"Your consultation appointment on {localStart:MMM dd, yyyy} at {localStart:hh:mm tt} was CANCELLED by your Trainer.{note}",
                NotificationType.Appointment);

            return (true, null);
        }
        catch (Exception exception) when (IsCancellationConflict(exception))
        {
            _context.ChangeTracker.Clear();
            return (false, "Appointment state changed while processing. Please try again.");
        }
    }

    private static bool IsApprovalConflict(Exception exception) =>
        exception is Microsoft.Data.SqlClient.SqlException { Number: 1205 or 2601 or 2627 }
        || exception is DbUpdateException { InnerException: Microsoft.Data.SqlClient.SqlException { Number: 1205 or 2601 or 2627 } };

    private static bool IsCancellationConflict(Exception exception) =>
        exception is Microsoft.Data.SqlClient.SqlException { Number: 1205 or 2601 or 2627 }
        || exception is DbUpdateException { InnerException: Microsoft.Data.SqlClient.SqlException { Number: 1205 or 2601 or 2627 } };

    public async Task<(bool Success, string? Error)> RescheduleAppointmentAsync(string trainerId, int appointmentId, int newSlotId)
    {
        var provider = await GetOrCreateTrainerProviderAsync(trainerId);
        var appointment = await _context.Appointments
            .Include(a => a.Slot)
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.Slot.ProviderId == provider.Id);

        if (appointment == null) return (false, "Appointment not found.");

        if (appointment.Status == AppointmentStatus.Cancelled)
            return (false, "Cannot reschedule a cancelled appointment.");

        if (appointment.Status == AppointmentStatus.Completed)
            return (false, "Cannot reschedule a completed appointment.");

        if (appointment.Slot.StartUtc <= _clock.GetUtcNow().UtcDateTime)
            return (false, "This appointment time has already passed.");

        var newSlot = await _context.AvailabilitySlots.Include(s => s.Provider)
            .FirstOrDefaultAsync(s => s.Id == newSlotId && s.ProviderId == provider.Id);

        if (newSlot == null || !newSlot.IsAvailable || !newSlot.Provider.IsActive)
            return (false, "The new appointment time is no longer available. Choose another time.");

        var nowUtc = _clock.GetUtcNow().UtcDateTime;
        if (newSlot.StartUtc <= nowUtc || newSlot.EndUtc <= newSlot.StartUtc)
            return (false, "Choose an appointment time in the future.");

        // Check for conflicts with serializable isolation to prevent concurrent bookings
        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var conflicts = _context.Appointments.Where(a => a.Status != AppointmentStatus.Cancelled
                && a.Slot.StartUtc < newSlot.EndUtc && a.Slot.EndUtc > newSlot.StartUtc);

            if (await conflicts.AnyAsync(a => a.Slot.ProviderId == newSlot.ProviderId && a.Id != appointmentId))
                return (false, "The provider is already booked at that time. Choose another time.");

            if (await conflicts.AnyAsync(a => a.ClientId == appointment.ClientId && a.Id != appointmentId))
                return (false, "The client already has an appointment that overlaps this time.");

            var oldSlot = appointment.Slot;
            var oldStart = _time.ToLocal(oldSlot.StartUtc);
            var newStart = _time.ToLocal(newSlot.StartUtc);

            appointment.SlotId = newSlotId;
            await CloseOpenRescheduleRequestsAsync(appointment.Id);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Notify client of reschedule with both old and new times
            await _notificationService.SendAsync(
                appointment.ClientId,
                $"Your consultation appointment was rescheduled from {oldStart:MMM dd, yyyy} at {oldStart:hh:mm tt} to {newStart:MMM dd, yyyy} at {newStart:hh:mm tt}.",
                NotificationType.Appointment);

            return (true, null);
        }
        catch (Exception exception) when (IsRescheduleConflict(exception))
        {
            _context.ChangeTracker.Clear();
            return (false, "Availability changed while you were rescheduling. Refresh and choose an available time.");
        }
    }

    // Loads an appointment and the open slots it can move to.
    public async Task<ReschedulePageViewModel?> LoadReschedulePageAsync(string trainerId, int appointmentId)
    {
        var provider = await GetOrCreateTrainerProviderAsync(trainerId);
        var appointment = await _context.Appointments
            .Include(a => a.Slot).ThenInclude(s => s.Provider)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.Slot.ProviderId == provider.Id);

        if (appointment == null) return null;

        var now = _clock.GetUtcNow().UtcDateTime;
        var slots = await _context.AvailabilitySlots.AsNoTracking()
            .Include(s => s.Provider)
            .Where(s => s.ProviderId == provider.Id && s.IsAvailable && s.StartUtc > now && s.EndUtc > s.StartUtc)
            .Where(s => !_context.Appointments.Any(a => a.Status != AppointmentStatus.Cancelled
                && a.Id != appointmentId
                && a.Slot.StartUtc < s.EndUtc && a.Slot.EndUtc > s.StartUtc))
            .OrderBy(s => s.StartUtc).ToListAsync();

        return new ReschedulePageViewModel
        {
            AppointmentId = appointmentId,
            NewSlotId = appointment.SlotId,
            ProviderName = appointment.Slot.Provider.Name,
            CurrentStartLocal = _time.ToLocal(appointment.Slot.StartUtc),
            CurrentEndLocal = _time.ToLocal(appointment.Slot.EndUtc),
            TimeZoneLabel = _time.Label,
            AvailableSlots = slots.Select(s => new AvailableSlotViewModel(s.Id, s.Provider.Name,
                _time.ToLocal(s.StartUtc), _time.ToLocal(s.EndUtc))).ToList()
        };
    }

    public Task<(bool Success, string? Error)> AcceptRescheduleRequestAsync(string trainerId, int requestId) =>
        DecideRescheduleRequestAsync(trainerId, requestId, accept: true, note: null);

    public Task<(bool Success, string? Error)> DeclineRescheduleRequestAsync(string trainerId, int requestId, string? note) =>
        DecideRescheduleRequestAsync(trainerId, requestId, accept: false, note);

    private async Task<(bool Success, string? Error)> DecideRescheduleRequestAsync(string trainerId, int requestId, bool accept, string? note)
    {
        var provider = await GetOrCreateTrainerProviderAsync(trainerId);
        note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (note?.Length > 500) return (false, "The note must be 500 characters or fewer.");

        // Serializable so a request cannot be accepted and declined at the same time.
        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var request = await _context.RescheduleRequests
                .Include(r => r.Appointment).ThenInclude(a => a.Slot)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.Appointment.Slot.ProviderId == provider.Id);

            if (request == null) return (false, "Reschedule request not found.");
            if (request.Status != RescheduleRequestStatus.Pending)
                return (false, "This reschedule request has already been answered.");
            if (request.Appointment.Status != AppointmentStatus.Approved
                || request.Appointment.Slot.StartUtc <= _clock.GetUtcNow().UtcDateTime)
            {
                request.Status = RescheduleRequestStatus.Closed;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (false, "This appointment is no longer upcoming, so the request was closed.");
            }

            request.Status = accept ? RescheduleRequestStatus.Accepted : RescheduleRequestStatus.Declined;
            request.TrainerNote = note;
            request.DecidedAtUtc = _clock.GetUtcNow().UtcDateTime;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var start = _time.ToLocal(request.Appointment.Slot.StartUtc);
            var message = accept
                ? $"Your trainer ACCEPTED your request to reschedule the consultation on {start:MMM dd, yyyy} at {start:hh:mm tt}. Open the appointment to choose a new time."
                : $"Your trainer DECLINED your request to reschedule the consultation on {start:MMM dd, yyyy} at {start:hh:mm tt}. The appointment stays as booked.{(note == null ? "" : " Note: " + note)}";
            if (message.Length > 300) message = message[..297] + "...";
            await _notificationService.SendAsync(request.Appointment.ClientId, message, NotificationType.Appointment);
            return (true, null);
        }
        catch (Exception exception) when (IsRescheduleConflict(exception))
        {
            _context.ChangeTracker.Clear();
            return (false, "The request changed while processing. Please try again.");
        }
    }

    // Requests stop applying once the trainer cancels or moves the appointment themselves.
    private async Task CloseOpenRescheduleRequestsAsync(int appointmentId)
    {
        var open = await _context.RescheduleRequests.Where(r => r.AppointmentId == appointmentId
            && (r.Status == RescheduleRequestStatus.Pending || r.Status == RescheduleRequestStatus.Accepted)).ToListAsync();
        foreach (var request in open)
        {
            request.Status = RescheduleRequestStatus.Closed;
            request.DecidedAtUtc ??= _clock.GetUtcNow().UtcDateTime;
        }
    }

    private static bool IsRescheduleConflict(Exception exception) =>
        exception is Microsoft.Data.SqlClient.SqlException { Number: 1205 or 2601 or 2627 }
        || exception is DbUpdateException { InnerException: Microsoft.Data.SqlClient.SqlException { Number: 1205 or 2601 or 2627 } };
}
