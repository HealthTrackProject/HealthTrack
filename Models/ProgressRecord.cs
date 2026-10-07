namespace HealthTrack.Models
{
    public class ProgressRecord
    {
        public int ProgressRecordId { get; set; }

        public string ClientId { get; set; } = string.Empty;

        public decimal Weight { get; set; }

        public decimal BMI { get; set; }

        public int CaloriesBurned { get; set; }

        public int WorkoutsCompleted { get; set; }

        public DateTime RecordDate { get; set; }

        public ApplicationUser? Client { get; set; }
    }
}