using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers;

// Shows trainer feedback to the client and handles weekly check-ins and replies.
public class ClientFeedbackController : ClientBaseController
{
    private readonly INotificationService _notifications;

    public ClientFeedbackController(HealthTrackDbContext context, UserManager<Person> users, INotificationService notifications) : base(context, users)
    {
        _notifications = notifications;
    }

    // Lists feedback and check-in requests from the client's trainer.
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

    // Submits the client's answer to a weekly check-in request.
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

        // Only the request that finds the check-in unanswered saves it and notifies the trainer,
        // so a double submit or two open tabs cannot answer twice.
        var saved = await Context.WeeklyCheckIns.Where(c => c.Id == id && c.ClientId == CurrentUserId && c.RespondedAtUtc == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Energy, energy).SetProperty(c => c.Mood, mood).SetProperty(c => c.Pain, pain)
                .SetProperty(c => c.Comment, comment).SetProperty(c => c.RespondedAtUtc, DateTime.UtcNow));
        if (saved == 0)
        {
            TempData["Error"] = "This check-in was already answered.";
            return RedirectToAction(nameof(Index));
        }
        await _notifications.SendAsync(checkIn.TrainerId,
            $"{checkIn.Client!.Name} answered the weekly check-in (energy {energy}/5, mood {mood}/5, pain {pain}/10).", NotificationType.General);
        TempData["Success"] = "Check-in sent to your Trainer.";
        return RedirectToAction(nameof(Index));
    }

    // Saves the client's one-time reply to a piece of feedback.
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

        if (!string.IsNullOrWhiteSpace(feedback.ClientReply))
        {
            TempData["Error"] = "You already replied to this feedback.";
            return RedirectToAction(nameof(Index));
        }

        var saved = await Context.TrainerFeedbacks.Where(f => f.Id == id && f.ClientId == CurrentUserId && f.ClientReply == null)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.ClientReply, reply).SetProperty(f => f.ClientRepliedAtUtc, DateTime.UtcNow));
        if (saved == 0)
        {
            TempData["Error"] = "You already replied to this feedback.";
            return RedirectToAction(nameof(Index));
        }
        await _notifications.SendAsync(feedback.TrainerId, $"Your client replied to your feedback \"{feedback.Title}\".", NotificationType.General);
        TempData["Success"] = "Reply sent.";
        return RedirectToAction(nameof(Index));
    }
}
