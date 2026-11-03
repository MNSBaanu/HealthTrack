using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services
{
    public class WeeklyReportSender(HealthTrackDbContext context, IExportService export, IMailSender mail, TimeProvider clock)
    {
        public async Task<int> SendAsync(CancellationToken cancellationToken = default)
        {
            if (!mail.IsConfigured) throw new InvalidOperationException("Email is not configured, so the report can't be sent.");
            var to = DateTime.Today;
            var from = to.AddDays(-6);
            var sections = await new AdminReportService(context).BuildSectionsAsync(from, to);
            var pdf = export.ToPdf(AdminReportService.PdfTitle, sections);
            var now = clock.GetUtcNow();
            var admins = await context.Admins.AsNoTracking()
                .Where(a => a.Email != null && (a.LockoutEnd == null || a.LockoutEnd <= now))
                .Select(a => new { a.Email, a.Name }).ToListAsync(cancellationToken);
            foreach (var admin in admins)
            {
                await mail.SendReportAsync(admin.Email!, admin.Name, $"HealthTrack weekly report, {from:d MMM} to {to:d MMM yyyy}",
                    $"Here is the General report for {from:d MMM} to {to:d MMM yyyy}. The PDF is attached.",
                    pdf, $"HealthTrack_Report_{to:yyyyMMdd}.pdf", cancellationToken);
            }

            var settings = await context.SystemSettings.FirstOrDefaultAsync(cancellationToken);
            if (settings == null)
            {
                settings = new SystemSettings();
                context.SystemSettings.Add(settings);
            }
            settings.WeeklyReportSentAtUtc = now.UtcDateTime;
            await context.SaveChangesAsync(cancellationToken);
            return admins.Count;
        }
    }
}
