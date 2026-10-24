namespace HealthTrack.Models.ViewModels;

public class AssignedRoutineReportRow
{
    public int PlanId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public int ExerciseCount { get; set; }
}

public class AdminReportsViewModel
{
    public List<ClientPerformanceViewModel> Clients { get; set; } = new();
    public List<ActivityStatViewModel> PersonalActivities { get; set; } = new();
    public List<AssignedRoutineReportRow> AssignedRoutines { get; set; } = new();
    public int ClientsWithCurrentTrainer { get; set; }
    public DateTime Today { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public string Status(AssignedRoutineReportRow plan) => ClientPlanStatus.Get(plan.IsActive, plan.StartDate, plan.EndDate, Today);
    public int ActivePlanCount => AssignedRoutines.Count(p => Status(p) == "Active");
    public int ClientsWithActivePlans => AssignedRoutines.Where(p => Status(p) == "Active").Select(p => p.ClientId).Distinct().Count();
    public int ClientsWithAnyPlans => AssignedRoutines.Select(p => p.ClientId).Distinct().Count();
    public double ActivePlanCoveragePercent => Clients.Count == 0 ? 0 : Math.Round(ClientsWithActivePlans * 100.0 / Clients.Count, 1);
}
