using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using HealthTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthTrack.Controllers
{
    public class TrainerScheduleController : TrainerBaseController
    {
        private readonly ITrainerScheduleService _scheduleService;

        public TrainerScheduleController(
            HealthTrackDbContext context,
            UserManager<Person> userManager,
            ITrainerScheduleService scheduleService)
            : base(context, userManager)
        {
            _scheduleService = scheduleService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _scheduleService.GetScheduleAsync(CurrentUserId);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddSlot(AddAvailabilitySlotForm form)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid time format for availability slot.";
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
    }
}
