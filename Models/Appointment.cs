namespace HealthTrack.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }

        public string ClientId { get; set; } = string.Empty;

        public string TrainerId { get; set; } = string.Empty;

        public DateTime AppointmentDate { get; set; }

        public string? Reason { get; set; }

        public string Status { get; set; } = "Pending";

        public ApplicationUser? Client { get; set; }

        public ApplicationUser? Trainer { get; set; }
    }
}