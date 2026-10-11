using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using HealthTrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AssignmentsController(HealthTrackDbContext context, UserManager<Person> users,
    TrainerAssignmentQueries queries, TimeProvider clock) : BaseController(context, users)
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, bool unassignedOnly = false)
    {
        var query = queries.AllClients();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.ClientName.Contains(search) || c.ClientEmail.Contains(search)
                || (c.TrainerName != null && c.TrainerName.Contains(search)));
        if (unassignedOnly) query = query.Where(c => c.TrainerId == null);
        return View(new AssignmentListViewModel
        {
            Clients = await query.OrderBy(c => c.ClientName).ToListAsync(),
            Search = search, UnassignedOnly = unassignedOnly
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var client = await Context.Clients.AsNoTracking().Include(c => c.TrainerAssignment)
            .ThenInclude(a => a!.Trainer).SingleOrDefaultAsync(c => c.Id == id);
        if (client == null) return NotFound();
        return View(await LoadPage(client, new AssignmentForm
        {
            ClientId = client.Id, TrainerId = client.TrainerAssignment?.TrainerId ?? string.Empty,
            RowVersion = client.TrainerAssignment == null ? null : Convert.ToBase64String(client.TrainerAssignment.RowVersion)
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(AssignmentForm form)
    {
        var client = await Context.Clients.Include(c => c.TrainerAssignment).ThenInclude(a => a!.Trainer)
            .SingleOrDefaultAsync(c => c.Id == form.ClientId);
        if (client == null) return NotFound();
        if (!await UserManager.IsInRoleAsync(client, AppRoles.Client))
            ModelState.AddModelError(string.Empty, "The selected account is not a valid Client.");
        var trainer = await Context.Trainers.SingleOrDefaultAsync(t => t.Id == form.TrainerId);
        if (trainer == null || !await UserManager.IsInRoleAsync(trainer, AppRoles.Trainer)
            || trainer.LockoutEnd > clock.GetUtcNow())
            ModelState.AddModelError(nameof(form.TrainerId), "Choose a valid, unlocked Trainer account.");
        var assignment = client.TrainerAssignment;
        if (!VersionMatches(assignment, form.RowVersion)) return Changed(form.ClientId);
        if (assignment?.TrainerId == form.TrainerId)
            ModelState.AddModelError(nameof(form.TrainerId), "This Trainer is already assigned to the Client.");
        if (!ModelState.IsValid) return View(await LoadPage(client, form));

        if (assignment == null)
        {
            Context.ClientTrainerAssignments.Add(new ClientTrainerAssignment
            {
                ClientId = client.Id, TrainerId = trainer!.Id, AssignedAtUtc = clock.GetUtcNow().UtcDateTime
            });
        }
        else
        {
            Context.Entry(assignment).Property(a => a.RowVersion).OriginalValue = Convert.FromBase64String(form.RowVersion!);
            assignment.TrainerId = trainer!.Id;
            assignment.AssignedAtUtc = clock.GetUtcNow().UtcDateTime;
        }
        try { await Context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Changed(form.ClientId); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 or 547 })
        { return Changed(form.ClientId); }
        TempData["Success"] = "Trainer assignment saved. Existing plans and appointments are unchanged.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Remove(RemoveAssignmentForm form)
    {
        if (!ModelState.IsValid) return BadRequest();
        var assignment = await Context.ClientTrainerAssignments.SingleOrDefaultAsync(a => a.ClientId == form.ClientId);
        if (assignment == null || !VersionMatches(assignment, form.RowVersion)) return Changed(form.ClientId);
        Context.Entry(assignment).Property(a => a.RowVersion).OriginalValue = Convert.FromBase64String(form.RowVersion);
        Context.ClientTrainerAssignments.Remove(assignment);
        try { await Context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Changed(form.ClientId); }
        TempData["Success"] = "Trainer assignment removed. Existing plans and appointments are unchanged.";
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Changed(string clientId)
    {
        TempData["Error"] = "The assignment or selected account changed. Review the current assignment and try again.";
        return RedirectToAction(nameof(Edit), new { id = clientId });
    }

    private static bool VersionMatches(ClientTrainerAssignment? assignment, string? version)
    {
        if (assignment == null) return string.IsNullOrEmpty(version);
        if (string.IsNullOrWhiteSpace(version)) return false;
        try { return assignment.RowVersion.SequenceEqual(Convert.FromBase64String(version)); }
        catch (FormatException) { return false; }
    }

    private async Task<AssignmentEditViewModel> LoadPage(Client client, AssignmentForm form)
    {
        var now = clock.GetUtcNow();
        var trainers = await Context.Trainers.AsNoTracking()
            .Where(t => t.LockoutEnd == null || t.LockoutEnd <= now)
            .Where(t => Context.UserRoles.Any(ur => ur.UserId == t.Id
                && Context.Roles.Any(r => r.Id == ur.RoleId && r.Name == AppRoles.Trainer)))
            .OrderBy(t => t.Name).Select(t => new SelectListItem
            { Value = t.Id, Text = t.Name + " (" + t.Email + ")" }).ToListAsync();
        return new AssignmentEditViewModel
        {
            ClientId = client.Id, ClientName = client.Name, ClientEmail = client.Email ?? string.Empty,
            CurrentTrainerName = client.TrainerAssignment?.Trainer.Name, Trainers = trainers,
            TrainerId = form.TrainerId, RowVersion = form.RowVersion
        };
    }
}
