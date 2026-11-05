using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models;

// Shared Identity entity; Trainer registration and dashboard belong to the Trainer module.
// Trainer user who manages assigned clients; inherits from Person.
public class Trainer : Person
{
    public ICollection<ClientTrainerAssignment> ClientAssignments { get; set; } = new List<ClientTrainerAssignment>();
    public ICollection<WorkoutPlan> WorkoutPlans { get; set; } = new List<WorkoutPlan>();
    public ICollection<DietPlan> DietPlans { get; set; } = new List<DietPlan>();
    public ICollection<TrainerFeedback> Feedbacks { get; set; } = new List<TrainerFeedback>();

    [NotMapped]
    public override string DashboardController => "Trainer";

    [NotMapped]
    public override string RoleDisplayName => "Trainer";
}
