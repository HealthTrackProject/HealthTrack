using System.Collections.Generic;

namespace HealthTrack.Models
{
    public class TrainerClientProgressViewModel
    {
        // Dropdown data
        public List<ClientOption> Clients { get; set; } = new List<ClientOption>();
        public string SelectedClientId { get; set; } = string.Empty;
        public int SelectedPeriod { get; set; } = 6; // months

        // Current stat cards
        public decimal CurrentWeight { get; set; }
        public decimal CurrentBMI { get; set; }
        public int CurrentCalories { get; set; }
        public int CurrentWorkouts { get; set; }

        // Chart data — keyed by metric then by period (3, 6, 12)
        public ProgressChartData ChartData { get; set; } = new ProgressChartData();
    }

    public class ClientOption
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class ProgressChartData
    {
        // labels per period
        public Dictionary<int, List<string>> Labels { get; set; } = new();

        // metric -> period -> values
        public ProgressMetric Weight { get; set; } = new();
        public ProgressMetric BMI { get; set; } = new();
        public ProgressMetric Calories { get; set; } = new();
        public ProgressMetric Workouts { get; set; } = new();
    }

    public class ProgressMetric
    {
        public string Label { get; set; } = string.Empty;
        public Dictionary<int, List<double>> Values { get; set; } = new();
        public Dictionary<int, string> Summary { get; set; } = new();
        public Dictionary<int, string> Change { get; set; } = new();
    }
}