using System.ComponentModel.DataAnnotations;

namespace PROGCMCS.Models
{
    public class CreateUserViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Hourly Rate")]
        [Range(50, 500, ErrorMessage = "Hourly rate must be between 50 and 500.")]
        public decimal HourlyRate { get; set; }

        [Required]
        [Display(Name = "Department")]
        public string Department { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } = string.Empty;

        // Auto-generated password (not displayed to HR)
        public string GeneratedPassword { get; set; } = string.Empty;
    }

    public class EditUserViewModel
    {
        public int LecturerId { get; set; }
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Hourly Rate")]
        [Range(50, 500, ErrorMessage = "Hourly rate must be between 50 and 500.")]
        public decimal HourlyRate { get; set; }

        [Required]
        [Display(Name = "Department")]
        public string Department { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public class UserListViewModel
    {
        public int LecturerId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}