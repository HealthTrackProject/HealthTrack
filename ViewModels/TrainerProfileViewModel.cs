using System;
using System.ComponentModel.DataAnnotations;

namespace HealthTrack.ViewModels
{
    public class TrainerManageProfileViewModel
    {
        // came from ApplicationUser 
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        public string? ProfilePictureUrl { get; set; }

        // came from HealthProfile 
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; } = DateTime.Now.AddYears(-20);

        [Range(1, 500, ErrorMessage = "Weight must be between 1 and 500 kg.")]
        public decimal Weight { get; set; }

        [Range(30, 300, ErrorMessage = "Height must be between 30 and 300 cm.")]
        public decimal Height { get; set; }

        public decimal BMI { get; set; }

        [Display(Name = "Medical Notes")]
        public string? MedicalNotes { get; set; }
    }
}