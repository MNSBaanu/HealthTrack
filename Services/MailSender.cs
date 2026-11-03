using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HealthTrack.Services
{
    public class MailSender : IMailSender
    {
        private readonly EmailOptions _options;
        private readonly ILogger<MailSender> _logger;

        public MailSender(IOptions<EmailOptions> options, ILogger<MailSender> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_options.Host) && !string.IsNullOrWhiteSpace(_options.FromAddress);

        // Sends in the background so a slow mail server never delays the page, and never throws into the caller.
        public void QueueNotification(string toAddress, string toName, string message)
        {
            if (!IsConfigured)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try { await SendNotificationAsync(toAddress, toName, message); }
                catch (Exception ex) { _logger.LogWarning(ex, "Could not send notification email"); }
            });
        }

        public async Task SendNotificationAsync(string toAddress, string toName, string message, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured) throw new InvalidOperationException("Email is not configured.");
            var mail = new MimeMessage();
            mail.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            mail.To.Add(new MailboxAddress(toName, toAddress));
            mail.Subject = "New notification from HealthTrack";
            mail.Body = new BodyBuilder
            {
                HtmlBody = BuildHtml(toName, message),
                TextBody = $"Hi {toName},\n\n{message}\n\nOpen HealthTrack to see more."
            }.ToMessageBody();
            await SendAsync(mail, cancellationToken);
        }

        public async Task SendReportAsync(string toAddress, string toName, string subject, string message,
            byte[] attachment, string fileName, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured) throw new InvalidOperationException("Email is not configured.");
            var mail = new MimeMessage();
            mail.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            mail.To.Add(new MailboxAddress(toName, toAddress));
            mail.Subject = subject;
            var body = new BodyBuilder
            {
                HtmlBody = BuildHtml(toName, message),
                TextBody = $"Hi {toName},\n\n{message}"
            };
            body.Attachments.Add(fileName, attachment, new ContentType("application", "pdf"));
            mail.Body = body.ToMessageBody();
            await SendAsync(mail, cancellationToken);
        }

        private async Task SendAsync(MimeMessage mail, CancellationToken cancellationToken)
        {
            using var client = new SmtpClient();
            client.Timeout = 30000;
            await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.Auto, cancellationToken);
            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            }
            await client.SendAsync(mail, cancellationToken);
            // A disconnect error after SMTP accepted the message must not cause a duplicate retry.
            try { await client.DisconnectAsync(true, cancellationToken); }
            catch (Exception ex) { _logger.LogDebug(ex, "SMTP disconnect failed after the notification was accepted"); }
        }

        private string BuildHtml(string name, string message)
        {
            var link = string.IsNullOrWhiteSpace(_options.SiteUrl)
                ? string.Empty
                : $"<p style=\"margin:24px 0 0\"><a href=\"{WebUtility.HtmlEncode(_options.SiteUrl.TrimEnd('/'))}/Notifications\" style=\"display:inline-block;padding:10px 20px;background:#e9a21b;color:#1a1823;font-weight:bold;text-decoration:none;border-radius:8px\">Open HealthTrack</a></p>";

            return $"""
                <div style="background:#f4efe6;padding:24px;font-family:Arial,Helvetica,sans-serif">
                  <div style="max-width:520px;margin:0 auto;background:#fffdf9;border:1px solid #e4dccf;border-radius:14px;overflow:hidden">
                    <div style="background:#1a1823;padding:18px 24px;font-size:20px;font-weight:bold;color:#f4efe6">
                      <span style="color:#e9a21b">&#9679;</span> Health<span style="color:#e9a21b;font-style:italic">Track</span>
                    </div>
                    <div style="padding:24px;color:#3a3642;font-size:15px;line-height:1.5">
                      <p style="margin:0 0 12px">Hi {WebUtility.HtmlEncode(name)},</p>
                      <p style="margin:0;color:#1a1823">{WebUtility.HtmlEncode(message)}</p>
                      {link}
                    </div>
                    <div style="padding:14px 24px;border-top:1px solid #e4dccf;color:#7a7482;font-size:12px">
                      You receive this because email notifications are on for your account. You can turn them off in your profile.
                    </div>
                  </div>
                </div>
                """;
        }
    }
}
