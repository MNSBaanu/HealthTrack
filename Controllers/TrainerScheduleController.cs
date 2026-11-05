using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using HealthTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthTrack.Controllers
{
    // Trainer availability and appointment management.
    public class TrainerScheduleController : TrainerBaseController
    {
        private readonly ITrainerScheduleService _scheduleService;
        private readonly AppointmentTime _time;

        public TrainerScheduleController(
            HealthTrackDbContext context,
            UserManager<Person> userManager,
            ITrainerScheduleService scheduleService,
            AppointmentTime time)
            : base(context, userManager)
        {
            _scheduleService = scheduleService;
            _time = time;
        }

        // Shows the trainer's slots, pending appointments and reschedule requests.
        public async Task<IActionResult> Index()
        {
            var model = await _scheduleService.GetScheduleAsync(CurrentUserId);
            return View(model);
        }

        // Adds an availability slot.
        [HttpPost]
        public async Task<IActionResult> AddSlot([Bind(Prefix = "NewSlotForm")] AddAvailabilitySlotForm form)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid time format for availability slot.";
                return RedirectToAction(nameof(Index));
            }

            // Additional validation to ensure both values are properly parsed
            if (form.StartLocal == DateTime.MinValue || form.EndLocal == DateTime.MinValue)
            {
                TempData["Error"] = "Please enter valid start and end times.";
                return RedirectToAction(nameof(Index));
            }

            var (success, error) = await _scheduleService.AddAvailabilitySlotAsync(CurrentUserId, form.StartLocal, form.EndLocal);
            if (!success)
            {
                TempData["Error"] = error;
            }
            else
            {
                TempData["Success"] = "Availability slot published successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Removes an unbooked slot.
        [HttpPost]
        public async Task<IActionResult> DeleteSlot(int slotId)
        {
            var (success, error) = await _scheduleService.DeleteSlotAsync(CurrentUserId, slotId);
            if (!success)
            {
                TempData["Error"] = error;
            }
            else
            {
                TempData["Success"] = "Availability slot removed.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Shows the edit form for a slot.
        [HttpGet]
        public async Task<IActionResult> EditSlot(int slotId)
        {
            var slot = await _scheduleService.GetSlotForEditAsync(CurrentUserId, slotId);
            if (slot == null) return NotFound();

            var model = new EditAvailabilitySlotForm
            {
                SlotId = slot.Id,
                StartLocal = _time.ToLocal(slot.StartUtc),
                EndLocal = _time.ToLocal(slot.EndUtc)
            };

            return View(model);
        }

        // Saves changes to a slot.
        [HttpPost]
        public async Task<IActionResult> EditSlot(EditAvailabilitySlotForm form)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid time format for availability slot.";
                return View(form);
            }

            if (form.StartLocal == DateTime.MinValue || form.EndLocal == DateTime.MinValue)
            {
                TempData["Error"] = "Please enter valid start and end times.";
                return View(form);
            }

            var (success, error) = await _scheduleService.UpdateSlotAsync(CurrentUserId, form.SlotId, form.StartLocal, form.EndLocal);
            if (!success)
            {
                TempData["Error"] = error;
                return View(form);
            }

            TempData["Success"] = "Availability slot updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Approves a pending appointment and notifies the client.
        [HttpPost]
        public async Task<IActionResult> Approve(int appointmentId)
        {
            var (success, error) = await _scheduleService.ApproveAppointmentAsync(CurrentUserId, appointmentId);
            if (!success)
            {
                TempData["Error"] = error;
            }
            else
            {
                TempData["Success"] = "Appointment approved. The client has been notified.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Cancels an appointment and notifies the client.
        [HttpPost]
        public async Task<IActionResult> Cancel(int appointmentId, string? reason)
        {
            var (success, error) = await _scheduleService.CancelAppointmentAsync(CurrentUserId, appointmentId, reason);
            if (!success)
            {
                TempData["Error"] = error;
            }
            else
            {
                TempData["Success"] = "Appointment cancelled. The client has been notified.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Moves an appointment to another slot and notifies the client.
        [HttpPost]
        public async Task<IActionResult> Reschedule(int appointmentId, int newSlotId)
        {
            var (success, error) = await _scheduleService.RescheduleAppointmentAsync(CurrentUserId, appointmentId, newSlotId);
            if (!success)
            {
                TempData["Error"] = error;
            }
            else
            {
                TempData["Success"] = "Appointment rescheduled. The client has been notified.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Accepts a client's reschedule request.
        [HttpPost]
        public async Task<IActionResult> AcceptRescheduleRequest(int requestId)
        {
            var (success, error) = await _scheduleService.AcceptRescheduleRequestAsync(CurrentUserId, requestId);
            if (!success)
            {
                TempData["Error"] = error;
            }
            else
            {
                TempData["Success"] = "Reschedule request accepted. The client can now choose a new time.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Declines a client's reschedule request with an optional note.
        [HttpPost]
        public async Task<IActionResult> DeclineRescheduleRequest(int requestId, string? note)
        {
            var (success, error) = await _scheduleService.DeclineRescheduleRequestAsync(CurrentUserId, requestId, note);
            if (!success)
            {
                TempData["Error"] = error;
            }
            else
            {
                TempData["Success"] = "Reschedule request declined. The client has been notified.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Shows open slots for moving an appointment.
        [HttpGet]
        public async Task<IActionResult> RescheduleAppointment(int appointmentId)
        {
            var model = await _scheduleService.LoadReschedulePageAsync(CurrentUserId, appointmentId);
            if (model == null) return NotFound();
            return View("Reschedule", model);
        }
    }
}
