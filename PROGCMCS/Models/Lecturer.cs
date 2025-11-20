using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROGCMCS.Models
{
    public class Lecturer
    {
        [Key]
        public int LecturerId { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Range(0, 1000)]
        public decimal HourlyRate { get; set; }

        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<MonthlyClaim> MonthlyClaims { get; set; } = new List<MonthlyClaim>();
    }
}