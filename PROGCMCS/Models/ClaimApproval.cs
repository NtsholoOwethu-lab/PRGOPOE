using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROGCMCS.Models
{
    public class ClaimApproval
    {
        [Key]
        public int ApprovalId { get; set; }

        [Required]
        public int ClaimId { get; set; }

        [Required]
        [StringLength(50)]
        public string ApproverRole { get; set; } = string.Empty; // Use this instead of ApproverRole

        [Required]
        public string ApproverId { get; set; } = string.Empty; // Identity User Id

        [Required]
        public bool IsApproved { get; set; }

        public DateTime ApprovalDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("ClaimId")]
        public virtual MonthlyClaim? MonthlyClaim { get; set; }
    }
}