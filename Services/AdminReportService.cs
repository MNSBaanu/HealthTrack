using System.Linq.Expressions;
using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services
{
    // Builds the data behind admin reports, exports and system health.
    public class AdminReportService(HealthTrackDbContext context, AppointmentTime time)
    {
        // Returns the display title for a report type.
        public static string Title(string? report) => report switch
        {
            "health" => "System health report",
            "ratings" => "Consultation ratings report",
            "audit" => "Audit log report",
            "chat" => "Chat questions report",
            "performance" => "Client performance report",
            _ => "General report"
        };

        // Builds the table sections for the selected report export.
        public async Task<List<ReportSection>> BuildSectionsAsync(string? report, DateTime? from, DateTime? to, bool emailConfigured,
            string? actor = null, string? action = null)
        {
            if (report == "performance") return await BuildPerformanceSectionsAsync(from, to);
            var sections = new List<ReportSection>();
            var general = report is not ("health" or "ratings" or "audit" or "chat");
            if (general) sections.AddRange(await BuildPerformanceSectionsAsync(from, to));
            if (general || report == "health") sections.AddRange(BuildHealthSections(await GetSystemHealthAsync(emailConfigured)));
            if (general || report == "ratings") sections.AddRange(BuildRatingsSections(await GetRatingsAsync(int.MaxValue)));
            if (general || report == "audit")
            {
                var (entries, names) = await GetAuditLogAsync(actor, action, from, to);
                sections.Add(new ReportSection("Audit Log",
                    new[] { "When", "Who", "Role", "Action", "Record", "Client", "Detail" },
                    entries.Select(e => new object?[]
                    {
                        e.CreatedAtUtc.ToLocalTime(), e.ActorName, e.ActorRole, e.Action, e.Entity,
                        e.SubjectClientId == null ? null : names.GetValueOrDefault(e.SubjectClientId, "Deleted account"), e.Detail
                    })));
            }
            if (general || report == "chat")
            {
                var (questions, faqs) = await GetChatQuestionsAsync();
                sections.Add(new ReportSection("Unanswered Chat Questions",
                    new[] { "Question", "Asked", "Asked by", "Last asked", "AI reply" },
                    GroupChatQuestions(questions).Select(q => new object?[] { q.Question, q.Count, q.Roles, q.Last.ToLocalTime(), q.Ai ? "Yes" : "No" })));
                sections.Add(new ReportSection("Saved Chatbot Answers",
                    new[] { "Question", "Answer" },
                    faqs.Select(f => new object?[] { f.Question, f.Answer })));
            }
            return sections;
        }

        // Collects user counts, sign-in activity and email status for the system health page.
        public async Task<SystemHealthViewModel> GetSystemHealthAsync(bool emailConfigured)
        {
            var now = DateTime.Now;
            var since7 = now.AddDays(-7);
            var firstDay = now.Date.AddDays(-13);
            var utcNow = DateTime.UtcNow;
            var logins = await context.LoginHistories.AsNoTracking()
                .Where(l => l.LoggedInAt >= firstDay)
                .Select(l => new { l.LoggedInAt, l.Succeeded, l.PersonId }).ToListAsync();

            return new SystemHealthViewModel
            {
                Clients = await context.Clients.CountAsync(),
                Trainers = await context.Trainers.CountAsync(),
                Admins = await context.Admins.CountAsync(),
                NewUsers7d = await context.Users.CountAsync(u => u.CreatedAt >= since7),
                LockedAccounts = await context.Users.CountAsync(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow),
                ActiveUsers7d = logins.Where(l => l.Succeeded && l.LoggedInAt >= since7).Select(l => l.PersonId).Distinct().Count(),
                FailedLogins24h = logins.Count(l => !l.Succeeded && l.LoggedInAt >= now.AddHours(-24)),
                FailedLogins7d = logins.Count(l => !l.Succeeded && l.LoggedInAt >= since7),
                Exports7d = await context.AuditLogs.CountAsync(a => a.Action == "Exported" && a.CreatedAtUtc >= utcNow.AddDays(-7)),
                Imports30d = await context.AuditLogs.CountAsync(a => a.Action == "Imported" && a.CreatedAtUtc >= utcNow.AddDays(-30)),
                PendingAppointments = await context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Pending),
                UnreadNotifications = await context.Notifications.CountAsync(n => !n.IsRead && !n.IsDismissed),
                EmailConfigured = emailConfigured,
                Daily = Enumerable.Range(0, 14).Select(i => firstDay.AddDays(i))
                    .Select(day => new DailyLogins(day,
                        logins.Count(l => l.Succeeded && l.LoggedInAt.Date == day),
                        logins.Count(l => !l.Succeeded && l.LoggedInAt.Date == day)))
                    .ToList()
            };
        }

        // Loads appointment ratings with client and provider names.
        public async Task<ProviderRatingsViewModel> GetRatingsAsync(int recent)
        {
            var rated = await context.Appointments.AsNoTracking()
                .Include(a => a.Slot).ThenInclude(s => s.Provider).Include(a => a.Client)
                .Where(a => a.Rating != null)
                .ToListAsync();

            return new ProviderRatingsViewModel
            {
                Providers = rated.Where(a => !a.RatingHidden).GroupBy(a => a.Slot.ProviderId)
                    .Select(g => new ProviderRatingRow(g.First().Slot.Provider.Name, g.First().Slot.Provider.Kind.ToString(),
                        g.First().Slot.Provider.Specialty, Math.Round(g.Average(a => a.Rating!.Value), 1), g.Count()))
                    .OrderByDescending(r => r.Average).ThenByDescending(r => r.Count).ToList(),
                Recent = rated.OrderByDescending(a => a.RatedAtUtc).Take(recent)
                    .Select(a => new RecentRating(a.Id, a.Slot.Provider.Name, a.Client.Name, a.Rating!.Value, a.RatingComment, time.ToLocal(a.RatedAtUtc!.Value), a.RatingHidden))
                    .ToList()
            };
        }

        public async Task<(List<AuditLog> Entries, Dictionary<string, string> Names)> GetAuditLogAsync(string? actor, string? action, DateTime? from, DateTime? to)
        {
            var logs = context.AuditLogs.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(actor)) logs = logs.Where(l => l.ActorName.Contains(actor.Trim()));
            if (!string.IsNullOrWhiteSpace(action)) logs = logs.Where(l => l.Action == action);
            if (from.HasValue) logs = logs.Where(l => l.CreatedAtUtc >= from.Value.Date.ToUniversalTime());
            if (to.HasValue) logs = logs.Where(l => l.CreatedAtUtc < to.Value.Date.AddDays(1).ToUniversalTime());
            var entries = await logs.OrderByDescending(l => l.CreatedAtUtc).Take(300).ToListAsync();
            var subjectIds = entries.Where(l => l.SubjectClientId != null).Select(l => l.SubjectClientId!).Distinct().ToList();
            var names = await context.Clients.Where(c => subjectIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name);
            return (entries, names);
        }

        public async Task<(List<ChatQuestion> Questions, List<ChatFaq> Faqs)> GetChatQuestionsAsync() =>
            (await context.ChatQuestions.AsNoTracking().OrderByDescending(q => q.CreatedAtUtc).Take(500).ToListAsync(),
             await context.ChatFaqs.AsNoTracking().OrderBy(f => f.Question).ToListAsync());

        // Groups similar chatbot questions and counts them.
        public static List<ChatQuestionGroup> GroupChatQuestions(IEnumerable<ChatQuestion> questions) =>
            questions.GroupBy(q => q.Question.Trim().ToLowerInvariant())
                .Select(g => new ChatQuestionGroup(g.First().Question, g.Count(), string.Join(", ", g.Select(q => q.Role).Distinct()),
                    g.Max(q => q.CreatedAtUtc), g.Any(q => q.AiAnswered)))
                .OrderByDescending(g => g.Count).ThenByDescending(g => g.Last)
                .ToList();

        private static IEnumerable<ReportSection> BuildHealthSections(SystemHealthViewModel health)
        {
            yield return new ReportSection("System Health",
                new[] { "Metric", "Value" },
                new[]
                {
                    new object?[] { "Clients", health.Clients },
                    new object?[] { "Trainers", health.Trainers },
                    new object?[] { "Admins", health.Admins },
                    new object?[] { "Active users (7 days)", health.ActiveUsers7d },
                    new object?[] { "New accounts (7 days)", health.NewUsers7d },
                    new object?[] { "Locked accounts", health.LockedAccounts },
                    new object?[] { "Failed sign-ins (24 hours)", health.FailedLogins24h },
                    new object?[] { "Failed sign-ins (7 days)", health.FailedLogins7d },
                    new object?[] { "Exports (7 days)", health.Exports7d },
                    new object?[] { "Imports (30 days)", health.Imports30d },
                    new object?[] { "Pending appointments", health.PendingAppointments },
                    new object?[] { "Unread notifications", health.UnreadNotifications },
                    new object?[] { "Email configured", health.EmailConfigured ? "Yes" : "No" }
                });
            yield return new ReportSection("Daily Sign-ins (14 days)",
                new[] { "Day", "Successful", "Failed" },
                health.Daily.Select(d => new object?[] { d.Day.ToString("yyyy-MM-dd"), d.Successful, d.Failed }));
        }

        private static IEnumerable<ReportSection> BuildRatingsSections(ProviderRatingsViewModel ratings)
        {
            yield return new ReportSection("Average Rating by Provider",
                new[] { "Provider", "Type", "Specialty", "Average", "Ratings" },
                ratings.Providers.Select(p => new object?[] { p.Name, p.Kind, p.Specialty, p.Average, p.Count }));
            yield return new ReportSection("Ratings",
                new[] { "Rated", "Provider", "Client", "Rating", "Comment", "Hidden" },
                ratings.Recent.Select(r => new object?[] { r.RatedAt, r.ProviderName, r.ClientName, r.Rating, r.Comment, r.Hidden ? "Yes" : "No" }));
        }

        private async Task<List<ReportSection>> BuildPerformanceSectionsAsync(DateTime? from, DateTime? to)
        {
            var performance = await GetClientPerformanceAsync(from, to);
            var activities = await GetPopularActivitiesAsync(10, from, to);
            var (planExercises, topPlans) = await GetPlanRoutinesAsync(10, from, to);

            return new List<ReportSection>
            {
                new ReportSection("Client Performance",
                    new[] { "Name", "Email", "Start Weight (kg)", "Current Weight (kg)", "Change (kg)", "BMI", "Workouts", "Minutes", "Calories Burned", "Meal Logs", "Weight Logs", "Appointments", "Active Days", "Last Login" },
                    performance.Select(p => new object?[]
                    {
                        p.Name, p.Email, p.StartWeightKg, p.CurrentWeightKg, p.WeightChangeKg, p.CurrentBmi,
                        p.WorkoutCount, p.TotalMinutes, p.TotalCaloriesBurned, p.MealLogCount, p.WeightLogCount, p.AppointmentCount, p.ActiveDays, p.LastLoginAt
                    })),
                new ReportSection("Popular Routines",
                    new[] { "Activity", "Sessions", "Clients", "Total Minutes" },
                    activities.Select(a => new object?[] { a.Activity, a.Sessions, a.ClientCount, a.TotalMinutes })),
                new ReportSection("Popular Plan Exercises",
                    new[] { "Exercise", "Plans", "Clients", "Completions" },
                    planExercises.Select(e => new object?[] { e.Exercise, e.PlanCount, e.ClientCount, e.Completions })),
                new ReportSection("Top Trainer Plans",
                    new[] { "Plan", "Trainer", "Client", "Exercises", "Completions" },
                    topPlans.Select(p => new object?[] { p.Title, p.Trainer, p.Client, p.ItemCount, p.Completions })),
                new ReportSection("Engagement",
                    new[] { "Metric", "Value" },
                    new[]
                    {
                        new object?[] { "Total clients", performance.Count },
                        new object?[] { "Clients active in last 7 days", performance.Count(p => p.LastLoginAt >= DateTime.Today.AddDays(-6)) },
                        new object?[] { "Clients with at least one workout", performance.Count(p => p.WorkoutCount > 0) },
                        new object?[] { "Average workouts per client", performance.Count == 0 ? 0 : Math.Round(performance.Average(p => p.WorkoutCount), 1) },
                        new object?[] { "Clients who logged a meal", performance.Count(p => p.MealLogCount > 0) },
                        new object?[] { "Clients who logged their weight", performance.Count(p => p.WeightLogCount > 0) },
                        new object?[] { "Appointments requested", performance.Sum(p => p.AppointmentCount) },
                        new object?[] { "Average active days per client", performance.Count == 0 ? 0 : Math.Round(performance.Average(p => p.ActiveDays), 1) }
                    })
            };
        }

        // Returns the most logged workout activities.
        public Task<List<ActivityStatViewModel>> GetPopularActivitiesAsync(int count, DateTime? from = null, DateTime? to = null)
        {
            var start = from?.Date;
            var end = to?.Date.AddDays(1);
            return context.WorkoutLogs
                .Where(w => (start == null || w.LoggedAt >= start) && (end == null || w.LoggedAt < end))
                .GroupBy(w => w.Activity)
                .Select(g => new ActivityStatViewModel
                {
                    Activity = g.Key,
                    Sessions = g.Count(),
                    ClientCount = g.Select(w => w.ClientId).Distinct().Count(),
                    TotalMinutes = g.Sum(w => w.DurationMinutes)
                })
                .OrderByDescending(a => a.Sessions)
                .Take(count)
                .ToListAsync();
        }

        public async Task<(List<PlanExerciseStatViewModel> Exercises, List<PlanStatViewModel> Plans)> GetPlanRoutinesAsync(int count, DateTime? from, DateTime? to)
        {
            var start = from?.Date;
            var end = to?.Date.AddDays(1);
            var completions = await context.WorkoutLogs
                .Where(w => w.WorkoutPlanItemId != null
                    && (start == null || w.LoggedAt >= start) && (end == null || w.LoggedAt < end))
                .Select(w => new { ItemId = w.WorkoutPlanItemId!.Value, w.ClientId, Date = w.LoggedAt.Date })
                .Union(context.WorkoutPlanCompletions
                    .Where(c => (start == null || c.CompletedOn >= start) && (end == null || c.CompletedOn < end))
                    .Select(c => new { ItemId = c.WorkoutPlanItemId, c.ClientId, Date = c.CompletedOn }))
                .GroupBy(w => w.ItemId)
                .Select(g => new { ItemId = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.ItemId, x => x.Total);
            var items = await context.WorkoutPlanItems.AsNoTracking()
                .Select(i => new
                {
                    i.Id,
                    i.ExerciseName,
                    i.WorkoutPlanId,
                    i.WorkoutPlan!.Title,
                    i.WorkoutPlan.ClientId,
                    ClientName = i.WorkoutPlan.Client!.Name,
                    TrainerName = i.WorkoutPlan.Trainer!.Name
                })
                .ToListAsync();

            var exercises = items
                .GroupBy(i => i.ExerciseName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new PlanExerciseStatViewModel
                {
                    Exercise = g.Key,
                    PlanCount = g.Select(i => i.WorkoutPlanId).Distinct().Count(),
                    ClientCount = g.Select(i => i.ClientId).Distinct().Count(),
                    Completions = g.Sum(i => completions.GetValueOrDefault(i.Id))
                })
                .OrderByDescending(e => e.Completions).ThenByDescending(e => e.PlanCount).ThenBy(e => e.Exercise)
                .Take(count).ToList();
            var plans = items
                .GroupBy(i => i.WorkoutPlanId)
                .Select(g => new PlanStatViewModel
                {
                    Title = g.First().Title,
                    Trainer = g.First().TrainerName,
                    Client = g.First().ClientName,
                    ItemCount = g.Count(),
                    Completions = g.Sum(i => completions.GetValueOrDefault(i.Id))
                })
                .OrderByDescending(p => p.Completions).ThenBy(p => p.Title)
                .Take(count).ToList();
            return (exercises, plans);
        }

        // Summarises each client's workouts, calories and weight change.
        public async Task<List<ClientPerformanceViewModel>> GetClientPerformanceAsync(DateTime? from = null, DateTime? to = null)
        {
            var start = from?.Date;
            var end = to?.Date.AddDays(1);
            var startUtc = start?.ToUniversalTime();
            var endUtc = end?.ToUniversalTime();
            Expression<Func<WorkoutLog, bool>> workoutInRange = w => (start == null || w.LoggedAt >= start) && (end == null || w.LoggedAt < end);
            Expression<Func<MealLog, bool>> mealInRange = m => (start == null || m.LoggedAt >= start) && (end == null || m.LoggedAt < end);
            Expression<Func<HealthMetric, bool>> metricInRange = m => (start == null || m.RecordedAt >= start) && (end == null || m.RecordedAt < end);

            var clients = await context.Clients
                .Select(c => new ClientPerformanceViewModel
                {
                    ClientId = c.Id,
                    Name = c.Name,
                    Email = c.Email ?? string.Empty,
                    HeightCm = c.HeightCm,
                    StartWeightKg = c.HealthMetrics.AsQueryable().Where(metricInRange).OrderBy(m => m.RecordedAt).Select(m => (double?)m.WeightKg).FirstOrDefault(),
                    CurrentWeightKg = c.HealthMetrics.AsQueryable().Where(metricInRange).OrderByDescending(m => m.RecordedAt).Select(m => (double?)m.WeightKg).FirstOrDefault(),
                    WeightLogCount = c.HealthMetrics.AsQueryable().Count(metricInRange),
                    WorkoutCount = c.WorkoutLogs.AsQueryable().Count(workoutInRange),
                    TotalMinutes = c.WorkoutLogs.AsQueryable().Where(workoutInRange).Sum(w => w.DurationMinutes),
                    TotalCaloriesBurned = c.WorkoutLogs.AsQueryable().Where(workoutInRange).Sum(w => w.CaloriesBurned),
                    MealLogCount = c.MealLogs.AsQueryable().Count(mealInRange),
                    LastLoginAt = c.LastLoginAt
                })
                .OrderBy(c => c.Name)
                .ToListAsync();

            var appointments = await context.Appointments
                .Where(a => (startUtc == null || a.RequestedAtUtc >= startUtc) && (endUtc == null || a.RequestedAtUtc < endUtc))
                .GroupBy(a => a.ClientId)
                .Select(g => new { ClientId = g.Key, Total = g.Count() })
                .ToDictionaryAsync(a => a.ClientId, a => a.Total);
            var activeDays = (await context.WorkoutLogs.Where(workoutInRange).Select(w => new { w.ClientId, w.LoggedAt.Date }).Distinct().ToListAsync())
                .Concat(await context.MealLogs.Where(mealInRange).Select(m => new { m.ClientId, m.LoggedAt.Date }).Distinct().ToListAsync())
                .Concat(await context.HealthMetrics.Where(metricInRange).Select(m => new { m.ClientId, m.RecordedAt.Date }).Distinct().ToListAsync())
                .Distinct()
                .GroupBy(d => d.ClientId)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var client in clients)
            {
                client.AppointmentCount = appointments.GetValueOrDefault(client.ClientId);
                client.ActiveDays = activeDays.GetValueOrDefault(client.ClientId);
            }
            return clients;
        }
    }
}
