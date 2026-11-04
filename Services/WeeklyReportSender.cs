using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services
{
    public class WeeklyReportSender(HealthTrackDbContext context, IExportService export, IMailSender mail, TimeProvider clock, AppointmentTime time)
    {
        public async Task<int> SendAsync(CancellationToken cancellationToken = default)
        {
            if (!mail.IsConfigured) throw new InvalidOperationException("Email is not configured, so the report can't be sent.");
            var to = time.ToLocal(clock.GetUtcNow().UtcDateTime).Date;
            var from = to.AddDays(-6);
            var sections = await new AdminReportService(context, time).BuildSectionsAsync("performance", from, to, mail.IsConfigured);
            var pdf = export.ToPdf("HealthTrack - " + AdminReportService.Title("performance"), sections);
            var now = clock.GetUtcNow();
            var admins = await context.Admins.AsNoTracking()
                .Where(a => a.Email != null && (a.LockoutEnd == null || a.LockoutEnd <= now))
                .Select(a => new { a.Email, a.Name }).ToListAsync(cancellationToken);
            // One bad mailbox must not stop the others, and a retry must not email admins twice.
            var sent = 0;
            Exception? firstFailure = null;
            foreach (var admin in admins)
            {
                try
                {
                    await mail.SendReportAsync(admin.Email!, admin.Name, $"HealthTrack weekly report, {from:d MMM} to {to:d MMM yyyy}",
                        $"Here is the client performance summary for {from:d MMM} to {to:d MMM yyyy}. The PDF is attached.",
                        pdf, $"HealthTrack_Report_{to:yyyyMMdd}.pdf", cancellationToken);
                    sent++;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    firstFailure ??= exception;
                }
            }
            if (sent == 0 && firstFailure != null) throw firstFailure;

            var settings = await context.SystemSettings.FirstOrDefaultAsync(cancellationToken);
            if (settings == null)
            {
                settings = new SystemSettings();
                context.SystemSettings.Add(settings);
            }
            settings.WeeklyReportSentAtUtc = now.UtcDateTime;
            await context.SaveChangesAsync(cancellationToken);
            return sent;
        }
    }
}
