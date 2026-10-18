using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HealthTrack.Views.ClientDashboard
{
    public class WorkoutModel : PageModel
    {
        public List<WorkoutDay> CurrentPlan { get; set; }

        public void OnGet()
        {
            // Mock data for the UI
            CurrentPlan = new List<WorkoutDay>
            {
                new WorkoutDay { Day = "Monday", MuscleGroup = "Chest & Arms", Exercises = "Push-ups, Bench Press, Bicep Curls" },
                new WorkoutDay { Day = "Tuesday", MuscleGroup = "Legs", Exercises = "Squats, Lunges, Leg Press" },
                new WorkoutDay { Day = "Wednesday", MuscleGroup = "Cardio", Exercises = "Running, Cycling, HIIT" },
                new WorkoutDay { Day = "Thursday", MuscleGroup = "Back & Shoulders", Exercises = "Deadlifts, Shoulder Press" },
                new WorkoutDay { Day = "Friday", MuscleGroup = "Full Body", Exercises = "Circuit Training" }
            };
        }

        public class WorkoutDay
        {
            public string Day { get; set; }
            public string MuscleGroup { get; set; }
            public string Exercises { get; set; }
        }
    }
}
