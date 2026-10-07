namespace HealthTrack.Models
{
    public class HealthRecord
    {
        public int HealthRecordId { get; set; }

        public string ClientId { get; set; } = string.Empty;

        public decimal Weight { get; set; }

        public decimal Height { get; set; }

        public decimal BMI { get; set; }

        public int CaloriesBurned { get; set; }

        public DateTime RecordDate { get; set; }

        public ApplicationUser? Client { get; set; }
    }
}