using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers;

public class ClientFeedbackController : ClientBaseController
{
    private readonly INotificationService _notifications;

    public ClientFeedbackController(HealthTrackDbContext context, UserManager<Person> users, INotificationService notifications) : base(context, users)
    {
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var feedback = await Context.TrainerFeedbacks.Include(f => f.Trainer)
            .Where(f => f.ClientId == CurrentUserId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync();

        var unread = feedback.Where(f => !f.IsReadByClient).Select(f => f.Id).ToList();
        if (unread.Count > 0)
        {
            await Context.TrainerFeedbacks.Where(f => unread.Contains(f.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(f => f.IsReadByClient, true));
        }

        ViewData["CheckIns"] = await Context.WeeklyCheckIns.AsNoTracking().Include(c => c.Trainer)
            .Where(c => c.ClientId == CurrentUserId)
            .OrderByDescending(c => c.RequestedAtUtc).Take(12).ToListAsync();

        return View(feedback);
    }

    [HttpPost]
    public async Task<IActionResult> CheckIn(int id, int energy, int mood, int pain, string? comment)
    {
        comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (energy is < 1 or > 5 || mood is < 1 or > 5 || pain is < 0 or > 10 || comment?.Length > 1000)
        {
            TempData["Error"] = "Please choose a value for energy, mood and pain. Comments can be at most 1000 characters.";
            return RedirectToAction(nameof(Index));
        }

        var checkIn = await Context.WeeklyCheckIns.Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.Id == id && c.ClientId == CurrentUserId);
        if (checkIn == null)
        {
            return NotFound();
        }
        if (checkIn.IsAnswered)
        {
            TempData["Error"] = "This check-in was already answered.";
            return RedirectToAction(nameof(Index));
        }

        checkIn.Energy = energy;
        checkIn.Mood = mood;
        checkIn.Pain = pain;
        checkIn.Comment = comment;
        checkIn.RespondedAtUtc = DateTime.UtcNow;
        await Context.SaveChangesAsync();
        await _notifications.SendAsync(checkIn.TrainerId,
            $"{checkIn.Client!.Name} answered the weekly check-in (energy {energy}/5, mood {mood}/5, pain {pain}/10).", NotificationType.General);
        TempData["Success"] = "Check-in sent to your Trainer.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Reply(int id, string reply)
    {
        reply = reply?.Trim() ?? string.Empty;
        if (reply.Length == 0 || reply.Length > 1000)
        {
            TempData["Error"] = "Reply must be between 1 and 1000 characters.";
            return RedirectToAction(nameof(Index));
        }

        var feedback = await Context.TrainerFeedbacks.FirstOrDefaultAsync(f => f.Id == id && f.ClientId == CurrentUserId);
        if (feedback == null)
        {
            return NotFound();
        }

        feedback.ClientReply = reply;
        feedback.ClientRepliedAtUtc = DateTime.UtcNow;
        await Context.SaveChangesAsync();
        await _notifications.SendAsync(feedback.TrainerId, $"Your client replied to your feedback \"{feedback.Title}\".", NotificationType.General);
        TempData["Success"] = "Reply sent.";
        return RedirectToAction(nameof(Index));
    }
}
