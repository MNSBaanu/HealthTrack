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
    public AppointmentsController(HealthTrackDbContext context, UserManager<Person> userManager,
        AppointmentBookingService booking, AppointmentTime time, TimeProvider clock) : base(context, userManager)
    {
        _booking = booking;
        _time = time;
        _clock = clock;
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
    public async Task<IActionResult> Book(int? providerId) => View(await LoadPage(new BookingForm(), providerId));

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
        return View(await LoadPage(form, null));
    }

    [HttpGet]
    public async Task<IActionResult> Reschedule(int appointmentId)
    {
        var appointment = await Context.Appointments.AsNoTracking()
            .Include(a => a.Slot).ThenInclude(s => s.Provider)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.ClientId == CurrentUserId);

        if (appointment == null) return NotFound();

        if (appointment.Status != AppointmentStatus.Pending)
        {
            TempData["Error"] = "Only pending appointments can be rescheduled by you.";
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

        TempData["Success"] = "Appointment rescheduled successfully. Your trainer will review the new time.";
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

    private async Task<BookingPageViewModel> LoadPage(BookingForm form, int? providerId)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var clientId = CurrentUserId;
        var slots = await Context.AvailabilitySlots.AsNoTracking().Include(s => s.Provider)
            .Where(s => s.Provider.IsActive && s.IsAvailable && s.StartUtc > now && s.EndUtc > s.StartUtc)
            .Where(s => providerId == null || s.ProviderId == providerId)
            .Where(s => !Context.Appointments.Any(a => a.Status != AppointmentStatus.Cancelled
                && (a.Slot.ProviderId == s.ProviderId || a.ClientId == clientId)
                && a.Slot.StartUtc < s.EndUtc && a.Slot.EndUtc > s.StartUtc))
            .OrderBy(s => s.StartUtc).ToListAsync();
        return new BookingPageViewModel
        {
            SlotId = form.SlotId, Notes = form.Notes, ProviderId = providerId, TimeZoneLabel = _time.Label,
            Providers = await Context.ConsultationProviders.AsNoTracking().Where(p => p.IsActive)
                .OrderBy(p => p.Name).ToListAsync(),
            Slots = slots.Select(s => new AvailableSlotViewModel(s.Id, s.Provider.Name,
                _time.ToLocal(s.StartUtc), _time.ToLocal(s.EndUtc))).ToList()
        };
    }
}
