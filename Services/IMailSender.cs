namespace HealthTrack.Services
{
    public interface IMailSender
    {
        bool IsConfigured { get; }
        void QueueNotification(string toAddress, string toName, string message);
    }
}
