using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models;

// Whether a provider is a trainer or a healthcare professional.
public enum ConsultationProviderKind { Trainer, HealthcareProfessional }

// Someone clients can book: a trainer or a healthcare professional.
public class ConsultationProvider
{
    public int Id { get; set; }
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
    [StringLength(200)]
    public string? Specialty { get; set; }
    public ConsultationProviderKind Kind { get; set; }
    public bool IsActive { get; set; } = true;
    // Link to the shared Identity account when the provider's module is integrated.
    public string? PersonId { get; set; }
    public Person? Person { get; set; }
    public ICollection<AvailabilitySlot> Slots { get; set; } = new List<AvailabilitySlot>();
}
