using System;
using System.Collections.Generic;

namespace HealthTrack.Models
{
    public class WorkoutPlan
    {
        public int WorkoutPlanId { get; set; }

        public string TrainerId { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public string PlanName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public ApplicationUser? Trainer { get; set; }

        public ApplicationUser? Client { get; set; }

        // One workout plan can have multiple workout days.
        public ICollection<WorkoutPlanDay> WorkoutPlanDays { get; set; }
            = new List<WorkoutPlanDay>();
    }
}