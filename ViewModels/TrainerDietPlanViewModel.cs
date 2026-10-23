using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HealthTrack.ViewModels
{
    public class TrainerDietPlanViewModel
    {
        [Required(ErrorMessage = "Please select a client.")]
        public string ClientId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a plan name.")]
        [StringLength(150)]
        public string PlanName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);

        public List<SelectListItem> Clients { get; set; } = new();

        public List<DietMealInputModel> Meals { get; set; } = new()
{
    new DietMealInputModel()
};
    }

    public class DietMealInputModel
    {
        [Required]
        public string MealType { get; set; } = string.Empty;

        public string? MealTime { get; set; }

        [Required(ErrorMessage = "Enter the food details.")]
        public string FoodDescription { get; set; } = string.Empty;

        [Range(0, 10000)]
        public int Calories { get; set; }

        public string? ImageUrl { get; set; }
    }
}