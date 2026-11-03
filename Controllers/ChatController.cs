using System.Text.RegularExpressions;
using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    [Authorize]
    [EnableRateLimiting("chat")]
    public class ChatController : BaseController
    {
        private static readonly Regex MedicalAdvice = new("diagnos|medicine|medication|symptom|treatment|medical advice|what should i eat|should i take", RegexOptions.IgnoreCase);
        private readonly ChatAiClient _ai;
        private readonly AppointmentTime _time;
        private readonly TimeProvider _clock;

        public ChatController(HealthTrackDbContext context, UserManager<Person> userManager, ChatAiClient ai,
            AppointmentTime time, TimeProvider clock) : base(context, userManager)
        {
            _ai = ai;
            _time = time;
            _clock = clock;
        }

        public record ChatRequest(string? Question);

        [HttpPost]
        [Authorize(Roles = AppRoles.Client)]
        public async Task<IActionResult> Own([FromBody] ChatRequest request)
        {
            var question = Normalize(request.Question);
            if (question.Length == 0) return Json(new { answer = (string?)null });
            var clientId = CurrentUserId;
            var today = DateTime.Today;

            if (Regex.IsMatch(question, @"\bbmi\b|body mass"))
            {
                var weight = await LatestWeightAsync(clientId);
                var height = await Context.Clients.Where(c => c.Id == clientId).Select(c => c.HeightCm).FirstAsync();
                if (weight == null) return Reply("You haven't recorded a weight yet, so I can't work out your BMI.", "/HealthMetrics", "Record weight");
                var bmi = HealthMetric.CalculateBmi(weight.Value, height);
                return Reply($"Your BMI is {bmi:0.0} ({HealthMetric.GetBmiCategory(bmi)}), from {weight.Value:0.#} kg and {height:0.#} cm.", "/HealthMetrics", "Open metrics");
            }
            if (Regex.IsMatch(question, @"\b(my|current|latest) weight\b|how much do i weigh"))
            {
                var latest = await Context.HealthMetrics.Where(m => m.ClientId == clientId)
                    .OrderByDescending(m => m.RecordedAt).Select(m => new { m.WeightKg, m.RecordedAt }).FirstOrDefaultAsync();
                if (latest == null) return Reply("You haven't recorded a weight yet.", "/HealthMetrics", "Record weight");
                return Reply($"Your latest weight is {latest.WeightKg:0.#} kg, recorded on {latest.RecordedAt:d MMM yyyy}.", "/HealthMetrics", "Open metrics");
            }
            if (Regex.IsMatch(question, @"next (appointment|consultation|booking)|upcoming (appointment|consultation)|when is my (appointment|consultation)"))
            {
                var now = _clock.GetUtcNow().UtcDateTime;
                var next = await Context.Appointments.AsNoTracking()
                    .Where(a => a.ClientId == clientId && a.Slot.StartUtc > now
                        && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved))
                    .OrderBy(a => a.Slot.StartUtc)
                    .Select(a => new { a.Slot.StartUtc, Provider = a.Slot.Provider.Name, a.Status })
                    .FirstOrDefaultAsync();
                if (next == null) return Reply("You have no upcoming appointments.", "/Appointments", "Book a consultation");
                return Reply($"Your next appointment is with {next.Provider} on {_time.ToLocal(next.StartUtc):ddd, d MMM yyyy HH:mm} ({_time.Label}). Status: {next.Status}.", "/Appointments", "Open appointments");
            }
            if (Regex.IsMatch(question, @"workouts? (this|last) week|how many workouts|my workouts"))
            {
                var weekStart = today.AddDays(-6);
                var week = await Context.WorkoutLogs.Where(w => w.ClientId == clientId && w.LoggedAt >= weekStart)
                    .GroupBy(w => 1).Select(g => new { Count = g.Count(), Minutes = g.Sum(w => w.DurationMinutes) }).FirstOrDefaultAsync();
                return Reply($"In the last 7 days you logged {week?.Count ?? 0} workout(s) totalling {week?.Minutes ?? 0} minutes.", "/WorkoutLogs", "Open workout journal");
            }
            if (Regex.IsMatch(question, @"calories (today|have i)|how many calories|my calories|eaten today"))
            {
                var calories = await Context.MealLogs.Where(m => m.ClientId == clientId && m.LoggedAt >= today).SumAsync(m => (int?)m.Calories) ?? 0;
                return Reply($"You've logged {calories:N0} kcal from meals today.", "/MealLogs", "Open meal journal");
            }
            if (Regex.IsMatch(question, @"my (trainer|coach)|who is my trainer"))
            {
                var trainer = await Context.ClientTrainerAssignments.Where(a => a.ClientId == clientId).Select(a => a.Trainer.Name).FirstOrDefaultAsync();
                return Reply(trainer == null ? "You don't have an assigned Trainer yet. An Admin manages assignments." : $"Your Trainer is {trainer}.", "/Client", "Open overview");
            }
            if (Regex.IsMatch(question, @"my feedback|new feedback|unread feedback|feedback (from|do i)"))
            {
                var unread = await Context.TrainerFeedbacks.CountAsync(f => f.ClientId == clientId && !f.IsReadByClient);
                return Reply(unread == 0 ? "You have no unread Trainer feedback." : $"You have {unread} unread Trainer feedback message(s).", "/ClientFeedback", "Open feedback");
            }
            return Json(new { answer = (string?)null });
        }

        [HttpPost]
        public async Task<IActionResult> Fallback([FromBody] ChatRequest request, CancellationToken cancellationToken)
        {
            var question = (request.Question ?? string.Empty).Trim();
            if (question.Length == 0) return Json(new { answer = (string?)null });
            if (question.Length > 300) question = question[..300];

            var key = FaqKey(question);
            var faq = (await Context.ChatFaqs.AsNoTracking().ToListAsync(cancellationToken))
                .FirstOrDefault(f =>
                {
                    var saved = FaqKey(f.Question);
                    return saved == key || (saved.Length >= 8 && key.Contains(saved)) || (key.Length >= 8 && saved.Contains(key));
                });
            if (faq != null) return Json(new { answer = faq.Answer });

            string? answer = null;
            if (!MedicalAdvice.IsMatch(question))
                answer = await _ai.AskAsync(question, cancellationToken);

            Context.ChatQuestions.Add(new ChatQuestion
            {
                Question = question,
                Role = User.IsInRole(AppRoles.Admin) ? "Admin" : User.IsInRole(AppRoles.Trainer) ? "Trainer" : "Client",
                AiAnswered = answer != null
            });
            await Context.SaveChangesAsync(cancellationToken);
            return Json(new { answer });
        }

        private static string Normalize(string? question) =>
            Regex.Replace((question ?? string.Empty).ToLowerInvariant(), @"[^a-z0-9\s]", " ").Trim();

        private static string FaqKey(string question) => Regex.Replace(Normalize(question), @"\s+", " ");

        private JsonResult Reply(string answer, string link, string linkText) =>
            Json(new { answer, link, linkText });

        private Task<double?> LatestWeightAsync(string clientId) =>
            Context.HealthMetrics.Where(m => m.ClientId == clientId).OrderByDescending(m => m.RecordedAt)
                .Select(m => (double?)m.WeightKg).FirstOrDefaultAsync();
    }
}
