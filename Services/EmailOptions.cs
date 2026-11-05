namespace HealthTrack.Services
{
    // SMTP settings read from configuration.
    public class EmailOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "HealthTrack";
        public string SiteUrl { get; set; } = string.Empty;
    }
}
