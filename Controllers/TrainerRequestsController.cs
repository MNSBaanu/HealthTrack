using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using HealthTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers;

// Lets a client ask the administrators for a trainer, or for a different one.
public class TrainerRequestsController(HealthTrackDbContext context, UserManager<Person> users,
    INotificationService notifications, TimeProvider clock) : ClientBaseController(context, users)
{
    private const string AlreadyPending = "You already have a trainer request waiting for an administrator.";

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (await HasPendingAsync())
        {
            TempData["Error"] = AlreadyPending;
            return RedirectToAction("Index", "Client");
        }
        return View(await LoadPage(new TrainerRequestForm()));
    }

    [HttpPost]
    public async Task<IActionResult> Create(TrainerRequestForm form)
    {
        var clientId = CurrentUserId;
        var assignment = await Context.ClientTrainerAssignments.AsNoTracking().Include(a => a.Trainer)
            .SingleOrDefaultAsync(a => a.ClientId == clientId);
        form.Note = string.IsNullOrWhiteSpace(form.Note) ? null : form.Note.Trim();
        form.PreferredTrainerId = string.IsNullOrWhiteSpace(form.PreferredTrainerId) ? null : form.PreferredTrainerId;

        if (assignment != null && form.Note == null)
            ModelState.AddModelError(nameof(form.Note), "Tell the administrator why you want a different trainer.");
        if (form.PreferredTrainerId != null)
        {
            if (form.PreferredTrainerId == assignment?.TrainerId)
                ModelState.AddModelError(nameof(form.PreferredTrainerId), "That is already your trainer. Choose a different one.");
            else if (!await IsAvailableTrainerAsync(form.PreferredTrainerId))
                ModelState.AddModelError(nameof(form.PreferredTrainerId), "Choose a trainer from the list.");
        }
        if (await HasPendingAsync()) ModelState.AddModelError(string.Empty, AlreadyPending);
        if (!ModelState.IsValid) return View(await LoadPage(form));

        Context.TrainerRequests.Add(new TrainerRequest
        {
            ClientId = clientId, PreferredTrainerId = form.PreferredTrainerId, IsChange = assignment != null,
            Note = form.Note, CreatedAtUtc = clock.GetUtcNow().UtcDateTime
        });
        try { await Context.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            ModelState.AddModelError(string.Empty, AlreadyPending);
            return View(await LoadPage(form));
        }

        var clientName = await Context.Users.Where(u => u.Id == clientId).Select(u => u.Name).SingleAsync();
        var preferredName = form.PreferredTrainerId == null ? null
            : await Context.Trainers.Where(t => t.Id == form.PreferredTrainerId).Select(t => t.Name).SingleOrDefaultAsync();
        var message = assignment == null
            ? $"{clientName} asked for a trainer"
            : $"{clientName} asked to change trainer from {assignment.Trainer.Name}";
        if (preferredName != null) message += $" (prefers {preferredName})";
        await notifications.SendToAllAdminsAsync($"{message}. Review it on Trainer assignments.", NotificationType.General);

        TempData["Success"] = "Your request was sent to the administrators. You'll be notified when they respond.";
        return RedirectToAction("Index", "Client");
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var request = await Context.TrainerRequests
            .SingleOrDefaultAsync(r => r.Id == id && r.ClientId == CurrentUserId && r.Status == TrainerRequestStatus.Pending);
        if (request == null)
        {
            TempData["Error"] = "That request is no longer waiting.";
            return RedirectToAction("Index", "Client");
        }
        request.Status = TrainerRequestStatus.Cancelled;
        request.ResolvedAtUtc = clock.GetUtcNow().UtcDateTime;
        await Context.SaveChangesAsync();
        TempData["Success"] = "Your trainer request was cancelled.";
        return RedirectToAction("Index", "Client");
    }

    private Task<bool> HasPendingAsync() =>
        Context.TrainerRequests.AnyAsync(r => r.ClientId == CurrentUserId && r.Status == TrainerRequestStatus.Pending);

    private IQueryable<Trainer> AvailableTrainers()
    {
        var now = clock.GetUtcNow();
        return Context.Trainers.AsNoTracking()
            .Where(t => t.LockoutEnd == null || t.LockoutEnd <= now)
            .Where(t => Context.UserRoles.Any(ur => ur.UserId == t.Id
                && Context.Roles.Any(r => r.Id == ur.RoleId && r.Name == AppRoles.Trainer)));
    }

    private Task<bool> IsAvailableTrainerAsync(string trainerId) => AvailableTrainers().AnyAsync(t => t.Id == trainerId);

    private async Task<TrainerRequestPageViewModel> LoadPage(TrainerRequestForm form)
    {
        var current = await Context.ClientTrainerAssignments.AsNoTracking()
            .Where(a => a.ClientId == CurrentUserId).Select(a => new { a.TrainerId, a.Trainer.Name }).SingleOrDefaultAsync();
        var trainers = await AvailableTrainers().Where(t => t.Id != (current == null ? null : current.TrainerId))
            .OrderBy(t => t.Name).Select(t => new SelectListItem { Value = t.Id, Text = t.Name }).ToListAsync();
        return new TrainerRequestPageViewModel
        {
            PreferredTrainerId = form.PreferredTrainerId, Note = form.Note,
            CurrentTrainerName = current?.Name, Trainers = trainers
        };
    }
}
