using System.Collections.Generic;

namespace HealthTrack.Models
{
    public class ClientDashboardViewModel
    {
        // User Details
        public string FullName { get; set; } = string.Empty;

        // Health Stats
        public decimal CurrentWeight { get; set; }
        public decimal CurrentBMI { get; set; }
        public int WeeklyCalories { get; set; }
        public int WeeklyWorkouts { get; set; }

        // Lists for Upcoming Activities & Charts
        public List<UpcomingActivity> UpcomingActivities { get; set; } = new List<UpcomingActivity>();
        public List<ChartDataPoint> WeightChartData { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> CaloriesChartData { get; set; } = new List<ChartDataPoint>();
    }

    public class UpcomingActivity
    {
        public string Title { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    public class ChartDataPoint
    {
        public string Label { get; set; } = string.Empty; // e.g., "Jan", "Mon"
        public double Value { get; set; }
    }
}