using System.Collections.Generic;

namespace HealthTrack.Models
{
    public class WorkoutViewModel
    {
        // Workout History that came from db
        public List<WorkoutLog> WorkoutHistory { get; set; } = new List<WorkoutLog>();

        // Current Plan that is currently being followed (could be a mock plan or one fetched from the database)
        public List<WorkoutDay> CurrentPlan { get; set; } = new List<WorkoutDay>();
    }

    public class WorkoutDay
    {
        public string Day { get; set; }
        public string MuscleGroup { get; set; }
        public string Exercises { get; set; }
    }
}