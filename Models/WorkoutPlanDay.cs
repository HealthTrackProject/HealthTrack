using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack.Models
{
    public class WorkoutPlanDay
    {
        [Key]
        public int WorkoutPlanDayId { get; set; }

        public int WorkoutPlanId { get; set; }

        public int DayNumber { get; set; }

        [Required]
        public string DayName { get; set; } = string.Empty;

        public string? MuscleGroup { get; set; }

        public string? Exercises { get; set; }

        [ForeignKey(nameof(WorkoutPlanId))]
        public WorkoutPlan WorkoutPlan { get; set; } = null!;
    }
}