using HealthTrack.Models;

namespace HealthTrack.Services
{
    // Creates, reads and generates in-app notifications.
    public interface INotificationService
    {
        Task SendAsync(string personId, string message, NotificationType type, DateTime? scheduledFor = null);
        Task SendToAllClientsAsync(string message, NotificationType type, DateTime? scheduledFor = null);
        Task SendToAllTrainersAsync(string message, NotificationType type, DateTime? scheduledFor = null);
        Task SendToAllAdminsAsync(string message, NotificationType type);
        Task<List<Notification>> GetVisibleAsync(string personId, bool unreadOnly);
        Task<int> GetUnreadCountAsync(string personId);
        Task GenerateDailyRemindersAsync(string clientId);
        Task GenerateAppointmentRemindersAsync(string clientId);
    }
}
