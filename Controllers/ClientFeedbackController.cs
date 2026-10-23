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

        return View(feedback);
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
