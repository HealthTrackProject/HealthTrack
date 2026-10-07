namespace HealthTrack.Models
{
    public class WorkoutLog
    {
        public int WorkoutLogId { get; set; }

        public int WorkoutPlanId { get; set; }

        public string ClientId { get; set; } = string.Empty;

        public string ExerciseName { get; set; } = string.Empty;

        public int DurationMinutes { get; set; }

        public int CaloriesBurned { get; set; }

        public DateTime WorkoutDate { get; set; }

        public WorkoutPlan? WorkoutPlan { get; set; }

        public ApplicationUser? Client { get; set; }
    }
}