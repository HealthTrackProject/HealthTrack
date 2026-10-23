using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    public class ClientFeedback
    {
        public int Id { get; set; }

        [Required]
        public string TrainerId { get; set; } = string.Empty;

        [Required]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? Trainer { get; set; }

        public ApplicationUser? Client { get; set; }
    }
}