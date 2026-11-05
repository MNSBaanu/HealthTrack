using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models;

// A sign-in attempt, kept for security review.
public class LoginHistory
{
    public int Id { get; set; }

    [Required]
    public string PersonId { get; set; } = string.Empty;
    public Person? Person { get; set; }

    public DateTime LoggedInAt { get; set; }

    public bool Succeeded { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(300)]
    public string? UserAgent { get; set; }
}
