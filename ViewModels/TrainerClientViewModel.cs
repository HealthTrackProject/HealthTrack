namespace HealthTrack.ViewModels
{
    public class TrainerClientViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }

        public int WorkoutsCompleted { get; set; }
        public decimal? Weight { get; set; }
        public decimal? BMI { get; set; }

        public string HealthSummary { get; set; } =
            "No health records available.";
    }
}