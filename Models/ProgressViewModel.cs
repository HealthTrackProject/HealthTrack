using System.Collections.Generic;

namespace HealthTrack.Models
{
    public class ProgressViewModel
    {
        // Top Stat Cards
        public decimal CurrentWeight { get; set; }
        public decimal CurrentBMI { get; set; }
        public int TotalCaloriesBurned { get; set; }
        public int TotalWorkouts { get; set; }

        // Weight Change (e.g., -5.2)
        public string WeightChange { get; set; } = "0";

        // Chart Data (Last 12 Months)
        public List<string> Labels { get; set; } = new List<string>();
        public List<double> WeightData { get; set; } = new List<double>();
        public List<double> BMIData { get; set; } = new List<double>();
        public List<double> CaloriesData { get; set; } = new List<double>();
        public List<double> WorkoutsData { get; set; } = new List<double>();
    }
}