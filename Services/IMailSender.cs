namespace HealthTrack.Services
{
    public interface IMailSender
    {
        bool IsConfigured { get; }
        void QueueNotification(string toAddress, string toName, string message);
        Task SendNotificationAsync(string toAddress, string toName, string message, CancellationToken cancellationToken = default);
    }
}
