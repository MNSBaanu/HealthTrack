using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models;

// Shared Identity entity; Trainer registration and dashboard belong to the Trainer module.
public class Trainer : Person
{
    public ICollection<ClientTrainerAssignment> ClientAssignments { get; set; } = new List<ClientTrainerAssignment>();
    [NotMapped]
    public override string RoleDisplayName => "Trainer";
}
