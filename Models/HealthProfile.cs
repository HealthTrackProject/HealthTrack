using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    public class HealthProfile
    {
        public int HealthProfileId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public decimal Weight { get; set; }

        public decimal Height { get; set; }

        public decimal BMI { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string? MedicalNotes { get; set; }

        public ApplicationUser? User { get; set; }
    }
}
