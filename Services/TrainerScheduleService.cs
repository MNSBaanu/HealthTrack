using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services;

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

    public async Task<ConsultationProvider> GetOrCreateTrainerProviderAsync(string trainerId)
    {
        var provider = await _context.ConsultationProviders
            .Include(p => p.Person)
            .FirstOrDefaultAsync(p => p.PersonId == trainerId);

        if (provider != null) return provider;

        var trainer = await _context.Trainers.FirstAsync(t => t.Id == trainerId);
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

    public async Task<TrainerScheduleViewModel> GetScheduleAsync(string trainerId)
    {
        var provider = await GetOrCreateTrainerProviderAsync(trainerId);
        var nowUtc = _clock.GetUtcNow().UtcDateTime;

        var slots = await _context.AvailabilitySlots
            .Where(s => s.ProviderId == provider.Id && s.EndUtc >= nowUtc)
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

        var pending = new List<TrainerAppointmentItemViewModel>();
        var approved = new List<TrainerAppointmentItemViewModel>();
        var pastOrCancelled = new List<TrainerAppointmentItemViewModel>();

        foreach (var appt in appointments)
        {
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
                RequestedAtLocal = _time.ToLocal(appt.RequestedAtUtc)
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
            UpcomingSlots = slots,
            PendingAppointments = pending,
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
            .FirstOrDefaultAsync(s => s.Id == slotId && s.ProviderId == provider.Id);

        if (slot == null) return (false, "Slot not found.");

        if (slot.Appointments.Any(a => a.Status != AppointmentStatus.Cancelled))
        {
            return (false, "Cannot delete a slot that has an active booking. Cancel the appointment first.");
        }

        _context.AvailabilitySlots.Remove(slot);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ApproveAppointmentAsync(string trainerId, int appointmentId)
    {
        var provider = await GetOrCreateTrainerProviderAsync(trainerId);
        var appointment = await _context.Appointments
            .Include(a => a.Slot)
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.Slot.ProviderId == provider.Id);

        if (appointment == null) return (false, "Appointment not found.");

        if (appointment.Status != AppointmentStatus.Pending)
        {
            return (false, $"Appointment is already {appointment.Status}.");
        }

        appointment.Status = AppointmentStatus.Approved;
        await _context.SaveChangesAsync();

        var localStart = _time.ToLocal(appointment.Slot.StartUtc);
        await _notificationService.SendAsync(
            appointment.ClientId,
            $"Your consultation appointment on {localStart:MMM dd, yyyy} at {localStart:hh:mm tt} was APPROVED by your Trainer.",
            NotificationType.Appointment);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CancelAppointmentAsync(string trainerId, int appointmentId, string? reason)
    {
        var provider = await GetOrCreateTrainerProviderAsync(trainerId);
        var appointment = await _context.Appointments
            .Include(a => a.Slot)
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.Slot.ProviderId == provider.Id);

        if (appointment == null) return (false, "Appointment not found.");

        appointment.Status = AppointmentStatus.Cancelled;
        await _context.SaveChangesAsync();

        var localStart = _time.ToLocal(appointment.Slot.StartUtc);
        var note = string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason.Trim()}";
        await _notificationService.SendAsync(
            appointment.ClientId,
            $"Your consultation appointment on {localStart:MMM dd, yyyy} at {localStart:hh:mm tt} was CANCELLED by your Trainer.{note}",
            NotificationType.Appointment);

        return (true, null);
    }
}
