using Microsoft.AspNetCore.Identity;

namespace HealthTrack.Models
{
    public class ApplicationUser:IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
