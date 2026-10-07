namespace HealthTrack.Models
{
    public class DietLog
    {
        public int DietLogId { get; set; }

        public int DietPlanId { get; set; }

        public string ClientId { get; set; } = string.Empty;

        public string MealType { get; set; } = string.Empty;

        public string FoodDescription { get; set; } = string.Empty;

        public int Calories { get; set; }

        public DateTime MealDate { get; set; }

        public DietPlan? DietPlan { get; set; }

        public ApplicationUser? Client { get; set; }
    }
}