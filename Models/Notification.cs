namespace HealthTrack.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; }

        public ApplicationUser? User { get; set; }
    }
}
