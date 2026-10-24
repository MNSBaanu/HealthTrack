using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using HealthTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers;

public class AppointmentsController : ClientBaseController
{
    private readonly AppointmentBookingService _booking;
    private readonly AppointmentTime _time;
    private readonly TimeProvider _clock;
    private readonly INotificationService _notifications;
    public AppointmentsController(HealthTrackDbContext context, UserManager<Person> userManager,
        AppointmentBookingService booking, AppointmentTime time, TimeProvider clock,
        INotificationService notifications) : base(context, userManager)
    {
        _booking = booking;
        _time = time;
        _clock = clock;
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var records = await Context.Appointments.AsNoTracking()
            .Include(a => a.Slot).ThenInclude(s => s.Provider)
            .Where(a => a.ClientId == CurrentUserId)
            .OrderByDescending(a => a.Slot.StartUtc).ToListAsync();
        return View(new AppointmentListViewModel
        {
            Appointments = records.Select(_time.ToViewModel).ToList(), TimeZoneLabel = _time.Label
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var record = await Context.Appointments.AsNoTracking()
            .Include(a => a.Slot).ThenInclude(s => s.Provider)
            .SingleOrDefaultAsync(a => a.Id == id && a.ClientId == CurrentUserId);
        if (record == null) return NotFound();
        ViewData["TimeZoneLabel"] = _time.Label;
        return View(await ToDetailsViewModel(record));
    }

    [HttpGet]
    public async Task<IActionResult> RequestReschedule(int appointmentId)
    {
        var record = await LoadOwnAppointment(appointmentId);
        if (record == null) return NotFound();
        var model = await ToDetailsViewModel(record);
        if (!model.CanRequestReschedule)
        {
            TempData["Error"] = "A reschedule request can't be sent for this appointment.";
            return RedirectToAction(nameof(Details), new { id = appointmentId });
        }
        return View(new RescheduleRequestPageViewModel { Appointment = model, TimeZoneLabel = _time.Label });
    }

    [HttpPost]
    public async Task<IActionResult> RequestReschedule(int appointmentId, RescheduleRequestForm form)
    {
        if (ModelState.IsValid)
        {
            var (success, error) = await _booking.RequestRescheduleAsync(CurrentUserId, appointmentId, form.Reason!);
            if (success)
            {
                TempData["Success"] = "Your reschedule request was sent. You'll be notified when your trainer responds.";
                return RedirectToAction(nameof(Details), new { id = appointmentId });
            }
            ModelState.AddModelError(string.Empty, error!);
        }

        var record = await LoadOwnAppointment(appointmentId);
        if (record == null) return NotFound();
        return View(new RescheduleRequestPageViewModel
        {
            Appointment = await ToDetailsViewModel(record), TimeZoneLabel = _time.Label, Reason = form.Reason
        });
    }

    private bool CanRate(Appointment record) =>
        record.Rating == null && (record.Status == AppointmentStatus.Completed
            || (record.Status == AppointmentStatus.Approved && record.Slot.EndUtc < _clock.GetUtcNow().UtcDateTime));

    [HttpPost]
    public async Task<IActionResult> Rate(int id, int rating, string? comment)
    {
        comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (rating is < 1 or > 5 || comment?.Length > 500)
        {
            TempData["Error"] = "Choose a rating from 1 to 5. Comments can be at most 500 characters.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var record = await Context.Appointments.Include(a => a.Slot)
            .SingleOrDefaultAsync(a => a.Id == id && a.ClientId == CurrentUserId);
        if (record == null) return NotFound();
        if (!CanRate(record))
        {
            TempData["Error"] = "This appointment cannot be rated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        record.Rating = rating;
        record.RatingComment = comment;
        record.RatedAtUtc = DateTime.UtcNow;
        await Context.SaveChangesAsync();
        TempData["Success"] = "Thanks for your rating.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Book(int? providerId, ConsultationProviderKind? kind) => View(await LoadPage(new BookingForm(), providerId, kind));

    [HttpPost]
    public async Task<IActionResult> Book(BookingForm form)
    {
        if (ModelState.IsValid)
        {
            var result = await _booking.BookAsync(CurrentUserId, form.SlotId!.Value, form.Notes);
            if (result.AppointmentId is int id)
            {
                TempData["Success"] = "Your consultation request was saved and is pending approval.";
                return RedirectToAction(nameof(Details), new { id });
            }
            ModelState.AddModelError(nameof(form.SlotId), result.Error!);
        }
        return View(await LoadPage(form, null, null));
    }

    [HttpPost]
    public async Task<IActionResult> RequestChange(int id, string requestType, string? message)
    {
        var record = await Context.Appointments.AsNoTracking()
            .Include(a => a.Client).Include(a => a.Slot).ThenInclude(s => s.Provider)
            .SingleOrDefaultAsync(a => a.Id == id && a.ClientId == CurrentUserId);
        if (record == null) return NotFound();
        if (record.Status != AppointmentStatus.Approved || record.Slot.Provider.PersonId == null || record.Slot.StartUtc <= _clock.GetUtcNow().UtcDateTime)
        {
            TempData["Error"] = "A change request can only be sent for an approved trainer appointment.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var action = requestType == "Reschedule" ? "RESCHEDULE" : "CANCEL";
        var start = _time.ToLocal(record.Slot.StartUtc);
        var note = string.IsNullOrWhiteSpace(message) ? "" : $" Message: {message.Trim()}";
        await _notifications.SendAsync(record.Slot.Provider.PersonId,
            $"{record.Client.Name} requested to {action} the consultation on {start:MMM dd, yyyy} at {start:hh:mm tt}.{note}",
            NotificationType.Appointment);
        TempData["Success"] = "Your request was sent to your trainer.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Reschedule(int appointmentId)
    {
        var appointment = await Context.Appointments.AsNoTracking()
            .Include(a => a.Slot).ThenInclude(s => s.Provider)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.ClientId == CurrentUserId);

        if (appointment == null) return NotFound();

        var state = await ToDetailsViewModel(appointment);
        if (!state.CanReschedule)
        {
            TempData["Error"] = state.RescheduleStatus switch
            {
                RescheduleRequestStatus.Pending => "Your reschedule request is still waiting for your trainer's decision.",
                RescheduleRequestStatus.Declined => "Your trainer declined your reschedule request, so this appointment can't be moved.",
                _ when appointment.Status == AppointmentStatus.Approved =>
                    "This appointment is approved. Send a reschedule request to your trainer first.",
                _ => "This appointment can no longer be rescheduled."
            };
            return RedirectToAction(nameof(Details), new { id = appointmentId });
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        var slots = await Context.AvailabilitySlots.AsNoTracking().Include(s => s.Provider)
            .Where(s => s.Provider.IsActive && s.IsAvailable && s.StartUtc > now && s.EndUtc > s.StartUtc)
            .Where(s => s.ProviderId == appointment.Slot.ProviderId)
            .Where(s => !Context.Appointments.Any(a => a.Status != AppointmentStatus.Cancelled
                && a.Id != appointmentId
                && a.Slot.StartUtc < s.EndUtc && a.Slot.EndUtc > s.StartUtc
                && (a.Slot.ProviderId == s.ProviderId || a.ClientId == CurrentUserId)))
            .OrderBy(s => s.StartUtc).ToListAsync();

        return View(new ReschedulePageViewModel
        {
            AppointmentId = appointmentId,
            ProviderName = appointment.Slot.Provider.Name,
            CurrentStartLocal = _time.ToLocal(appointment.Slot.StartUtc),
            CurrentEndLocal = _time.ToLocal(appointment.Slot.EndUtc),
            TimeZoneLabel = _time.Label,
            AvailableSlots = slots.Select(s => new AvailableSlotViewModel(s.Id, s.Provider.Name,
                _time.ToLocal(s.StartUtc), _time.ToLocal(s.EndUtc))).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Reschedule(int appointmentId, RescheduleForm form)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please select a valid appointment time.";
            return RedirectToAction(nameof(Reschedule), new { appointmentId });
        }

        var (success, error) = await _booking.RescheduleClientAppointmentAsync(CurrentUserId, appointmentId, form.NewSlotId!.Value);
        
        if (!success)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Reschedule), new { appointmentId });
        }

        TempData["Success"] = "Appointment rescheduled successfully.";
        return RedirectToAction(nameof(Details), new { id = appointmentId });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int appointmentId)
    {
        var (success, error) = await _booking.CancelClientAppointmentAsync(CurrentUserId, appointmentId);
        
        if (!success)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Details), new { id = appointmentId });
        }

        TempData["Success"] = "Appointment cancelled successfully.";
        return RedirectToAction(nameof(Index));
    }

    private Task<Appointment?> LoadOwnAppointment(int appointmentId) =>
        Context.Appointments.AsNoTracking()
            .Include(a => a.Slot).ThenInclude(s => s.Provider)
            .SingleOrDefaultAsync(a => a.Id == appointmentId && a.ClientId == CurrentUserId);

    // Adds the reschedule-request state and decides which actions the client may take.
    private async Task<AppointmentViewModel> ToDetailsViewModel(Appointment record)
    {
        var model = _time.ToViewModel(record);
        var latest = await Context.RescheduleRequests.AsNoTracking()
            .Where(r => r.AppointmentId == record.Id)
            .OrderByDescending(r => r.RequestedAtUtc).FirstOrDefaultAsync();
        model.RescheduleStatus = latest?.Status;
        model.RescheduleReason = latest?.Reason;
        model.RescheduleTrainerNote = latest?.TrainerNote;

        var isUpcoming = record.Slot.StartUtc > _clock.GetUtcNow().UtcDateTime;
        model.IsUpcoming = isUpcoming;
        var isOpen = latest?.Status is RescheduleRequestStatus.Pending or RescheduleRequestStatus.Accepted;
        model.CanRequestReschedule = record.Status == AppointmentStatus.Approved && isUpcoming && !isOpen
            && record.Slot.Provider.Kind == ConsultationProviderKind.Trainer && record.Slot.Provider.PersonId != null;
        model.CanReschedule = (record.Status == AppointmentStatus.Pending && isUpcoming)
            || (record.Status == AppointmentStatus.Approved && isUpcoming && latest?.Status == RescheduleRequestStatus.Accepted);
        return model;
    }

    private async Task<BookingPageViewModel> LoadPage(BookingForm form, int? providerId, ConsultationProviderKind? kind)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var clientId = CurrentUserId;
        var slots = await Context.AvailabilitySlots.AsNoTracking().Include(s => s.Provider)
            .Where(s => s.Provider.IsActive && s.IsAvailable && s.StartUtc > now && s.EndUtc > s.StartUtc)
            .Where(s => providerId == null || s.ProviderId == providerId)
            .Where(s => kind == null || s.Provider.Kind == kind)
            .Where(s => !Context.Appointments.Any(a => a.Status != AppointmentStatus.Cancelled
                && (a.Slot.ProviderId == s.ProviderId || a.ClientId == clientId)
                && a.Slot.StartUtc < s.EndUtc && a.Slot.EndUtc > s.StartUtc))
            .OrderBy(s => s.StartUtc).ToListAsync();
        return new BookingPageViewModel
        {
            SlotId = form.SlotId, Notes = form.Notes, ProviderId = providerId, Kind = kind, TimeZoneLabel = _time.Label,
            Providers = await Context.ConsultationProviders.AsNoTracking().Where(p => p.IsActive)
                .OrderBy(p => p.Kind).ThenBy(p => p.Name).ToListAsync(),
            Slots = slots.Select(s => new AvailableSlotViewModel(s.Id, s.Provider.Name,
                _time.ToLocal(s.StartUtc), _time.ToLocal(s.EndUtc))).ToList()
        };
    }
}
