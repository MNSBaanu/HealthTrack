using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    // A meal logged by a client.
    public class MealLog
    {
        [Key]
        public int MealLogId { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;

        public Client? Client { get; set; }

        public MealType MealType { get; set; }

        [Range(0, 10000)]
        public int Calories { get; set; }

        [StringLength(500)]
        public string? FoodItems { get; set; }

        public DateTime LoggedAt { get; set; } = DateTime.Now;

        public int? DietPlanMealId { get; set; }

        public DietPlanMeal? DietPlanMeal { get; set; }
    }
}
