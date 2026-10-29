namespace HealthTrack.Models.ViewModels;

public class JournalFilterViewModel
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Q { get; set; }
    public MealType? MealType { get; set; }
    public bool IsMealJournal { get; set; }
    public bool HasFilters => From.HasValue || To.HasValue || !string.IsNullOrWhiteSpace(Q) || MealType.HasValue;
}
