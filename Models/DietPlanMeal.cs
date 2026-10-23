using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    public class DietPlanMeal
    {
        [Key]
        public int DietPlanMealId { get; set; }

        public int DietPlanId { get; set; }

        [Required]
        [StringLength(50)]
        public string MealType { get; set; } = string.Empty;

        [StringLength(30)]
        public string? MealTime { get; set; }

        [Required]
        public string FoodDescription { get; set; } = string.Empty;

        public int Calories { get; set; }

        public string? ImageUrl { get; set; }

        [ForeignKey(nameof(DietPlanId))]
        public DietPlan DietPlan { get; set; } = null!;
    }
}