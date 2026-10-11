namespace HealthTrack.Models;

public class AvailabilitySlot
{
    public int Id { get; set; }
    public int ProviderId { get; set; }
    public ConsultationProvider Provider { get; set; } = null!;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public bool IsAvailable { get; set; } = true;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
