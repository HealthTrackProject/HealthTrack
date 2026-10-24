using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthTrack.ViewModels
{
    public class ManageUsersViewModel
    {
        public List<UserRowViewModel> Users { get; set; } = new();

        public AddUserViewModel NewUser { get; set; } = new();

        public EditUserViewModel EditUser { get; set; } = new();
    }

    public class UserRowViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = "Client";

        public string? ProfileImage { get; set; }
    }

    public class AddUserViewModel
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression(
            "^(Trainer|Admin)$",
            ErrorMessage = "Only Trainer or Admin can be created here.")]
        public string Role { get; set; } = "Trainer";
    }

    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;
    }
}