using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthTrack.ViewModels
{
    public class TrainerWorkoutPlanViewModel
    {
        [Required(ErrorMessage = "Please select a client.")]
        public string ClientId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the plan name.")]
        public string PlanName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);

        public List<SelectListItem> Clients { get; set; } = new();

        public List<WorkoutDayViewModel> Days { get; set; } = new()
        {
            new WorkoutDayViewModel
            {
                DayNumber = 1,
                DayName = "Monday",
                MuscleGroup = "Chest & Arms",
                Exercises = "Push-ups, Bench Press, Bicep Curls"
            }
        };
    }

    public class WorkoutDayViewModel
    {
        public int DayNumber { get; set; }

        public string DayName { get; set; } = string.Empty;

        public string MuscleGroup { get; set; } = string.Empty;

        public string Exercises { get; set; } = string.Empty;
    }
}