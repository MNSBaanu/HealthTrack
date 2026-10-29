using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Services;

public class AdminBroadcastService(HealthTrackDbContext context, AppointmentTime time, TimeProvider clock)
{
    public async Task SendAsync(string senderId, string senderName, string? recipientId,
        string message, NotificationType type, DateTime? scheduledFor)
    {
        var recipients = context.Users.AsQueryable();
        if (string.IsNullOrEmpty(recipientId)) recipients = recipients.OfType<Client>();
        else if (recipientId == "all-trainers") recipients = recipients.OfType<Trainer>();
        else recipients = recipients.Where(p => p.Id == recipientId && (p is Client || p is Trainer));
        var people = await recipients.Select(p => new { p.Id, p.Name, p.Email, p.EmailNotificationsEnabled }).ToListAsync();
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var broadcast = new AdminBroadcast
        {
            SenderId = senderId, SenderName = senderName, Message = message.Trim(), Type = type,
            CreatedAtUtc = nowUtc, ScheduledFor = scheduledFor, RecipientCount = people.Count,
            RecipientDescription = string.IsNullOrEmpty(recipientId) ? "All clients"
                : recipientId == "all-trainers" ? "All trainers" : people.FirstOrDefault()?.Name ?? "Recipient unavailable"
        };
        foreach (var person in people)
        {
            var notification = new Notification
            {
                PersonId = person.Id, Message = broadcast.Message, Type = type,
                CreatedAt = time.ToLocal(nowUtc), ScheduledFor = scheduledFor
            };
            if (person.EmailNotificationsEnabled && !string.IsNullOrWhiteSpace(person.Email))
                notification.EmailDelivery = new NotificationEmail
                    { DueAtUtc = scheduledFor.HasValue ? time.ToUtc(scheduledFor.Value) : nowUtc };
            broadcast.Notifications.Add(notification);
        }
        context.AdminBroadcasts.Add(broadcast);
        await context.SaveChangesAsync();
    }
}
