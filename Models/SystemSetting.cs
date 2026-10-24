using System.ComponentModel.DataAnnotations;

namespace HealthTrack.Models
{
    public class SystemSetting
    {
        [Key]
        public int Id { get; set; }

        // General Settings
        public string SystemName { get; set; } = "HealthTrack System";
        public string SupportEmail { get; set; } = "support@healthtrack.com";
        public string DefaultTimezone { get; set; } = "IST";

        // Security & Privacy
        public bool TwoFactorAuth { get; set; } = true;
        public bool MaintenanceMode { get; set; } = false;
        public bool ForcePasswordReset { get; set; } = false;

        // Notification Settings
        public bool EmailNotifications { get; set; } = true;
        public bool SmsAlerts { get; set; } = false;

        // System Preferences
        public int MaxUploadSize { get; set; } = 10;
        public bool AllowUserRegistration { get; set; } = true;
    }
}