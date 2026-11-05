namespace HealthTrack.Services
{
    // Sends notification and report emails.
    public interface IMailSender
    {
        bool IsConfigured { get; }
        void QueueNotification(string toAddress, string toName, string message);
        Task SendNotificationAsync(string toAddress, string toName, string message, CancellationToken cancellationToken = default);
        Task SendReportAsync(string toAddress, string toName, string subject, string message, byte[] attachment, string fileName, CancellationToken cancellationToken = default);
    }
}
