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
        return View(_time.ToViewModel(record));
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
    public async Task<IActionResult> Cancel(int id)
    {
        var error = await _booking.CancelAsync(CurrentUserId, id);
        if (error == null) TempData["Success"] = "Your appointment was cancelled.";
        else TempData["Error"] = error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Reschedule(int id)
    {
        var model = await LoadReschedulePage(id);
        if (model == null) return NotFound();
        return model.NewSlotId == null ? RedirectToAction(nameof(Details), new { id }) : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Reschedule(int id, RescheduleForm form)
    {
        if (ModelState.IsValid)
        {
            var error = await _booking.RescheduleAsync(CurrentUserId, id, form.NewSlotId!.Value);
            if (error == null)
            {
                TempData["Success"] = "Your appointment was rescheduled.";
                return RedirectToAction(nameof(Details), new { id });
            }
            ModelState.AddModelError(nameof(form.NewSlotId), error);
        }
        var model = await LoadReschedulePage(id);
        if (model == null) return NotFound();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> RequestChange(int id, string requestType, string? message)
    {
        var record = await Context.Appointments.AsNoTracking()
            .Include(a => a.Client).Include(a => a.Slot).ThenInclude(s => s.Provider)
            .SingleOrDefaultAsync(a => a.Id == id && a.ClientId == CurrentUserId);
        if (record == null) return NotFound();
        if (record.Status != AppointmentStatus.Approved || record.Slot.Provider.PersonId == null)
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

    private async Task<ReschedulePageViewModel?> LoadReschedulePage(int id)
    {
        var clientId = CurrentUserId;
        var record = await Context.Appointments.AsNoTracking()
            .Include(a => a.Slot).ThenInclude(s => s.Provider)
            .SingleOrDefaultAsync(a => a.Id == id && a.ClientId == clientId);
        if (record == null) return null;

        var model = new ReschedulePageViewModel
        {
            AppointmentId = id, ProviderName = record.Slot.Provider.Name, TimeZoneLabel = _time.Label,
            CurrentStartLocal = _time.ToLocal(record.Slot.StartUtc), CurrentEndLocal = _time.ToLocal(record.Slot.EndUtc)
        };
        if (record.Status != AppointmentStatus.Pending) return model;

        var now = _clock.GetUtcNow().UtcDateTime;
        var slots = await Context.AvailabilitySlots.AsNoTracking().Include(s => s.Provider)
            .Where(s => s.ProviderId == record.Slot.ProviderId && s.Provider.IsActive && s.IsAvailable
                && s.StartUtc > now && s.EndUtc > s.StartUtc)
            .Where(s => !Context.Appointments.Any(a => a.Id != id && a.Status != AppointmentStatus.Cancelled
                && (a.Slot.ProviderId == s.ProviderId || a.ClientId == clientId)
                && a.Slot.StartUtc < s.EndUtc && a.Slot.EndUtc > s.StartUtc))
            .OrderBy(s => s.StartUtc).ToListAsync();
        model.NewSlotId = record.SlotId;
        model.AvailableSlots = slots.Where(s => s.Id != record.SlotId).Select(s => new AvailableSlotViewModel(s.Id, s.Provider.Name,
            _time.ToLocal(s.StartUtc), _time.ToLocal(s.EndUtc))).ToList();
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
