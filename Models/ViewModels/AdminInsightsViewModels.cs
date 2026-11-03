namespace HealthTrack.Models.ViewModels
{
    public record ProviderRatingRow(string Name, string Kind, string? Specialty, double Average, int Count);

    public record RecentRating(string ProviderName, string ClientName, int Rating, string? Comment, DateTime RatedAt);

    public record AdminAppointmentRow(int Id, string ClientId, string ClientName, string ProviderName, ConsultationProviderKind ProviderKind, DateTime Start, AppointmentStatus Status, DateTime RequestedAt);

    public class ProviderRatingsViewModel
    {
        public List<ProviderRatingRow> Providers { get; set; } = new();
        public List<RecentRating> Recent { get; set; } = new();
    }

    public record DailyLogins(DateTime Day, int Successful, int Failed);

    public class SystemHealthViewModel
    {
        public int Clients { get; set; }
        public int Trainers { get; set; }
        public int Admins { get; set; }
        public int NewUsers7d { get; set; }
        public int ActiveUsers7d { get; set; }
        public int LockedAccounts { get; set; }
        public int FailedLogins24h { get; set; }
        public int FailedLogins7d { get; set; }
        public int Exports7d { get; set; }
        public int Imports30d { get; set; }
        public int PendingAppointments { get; set; }
        public int UnreadNotifications { get; set; }
        public bool EmailConfigured { get; set; }
        public List<DailyLogins> Daily { get; set; } = new();
    }
}
