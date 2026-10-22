using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers;

public class ClientFeedbackController : ClientBaseController
{
    public ClientFeedbackController(HealthTrackDbContext context, UserManager<Person> users) : base(context, users) { }

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
}
