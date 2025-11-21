using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROGCMCS.Models
{
    public enum ApproverType
    {
        ProgrammeCoordinator,
        AcademicManager
    }

    public class ClaimApproval
    {
        [Key]
        public int ApprovalId { get; set; }

        [Required]
        public int ClaimId { get; set; }

        [Required]
        public ApproverType ApproverType { get; set; }

        [Required]
        public int ApproverId { get; set; }

        public bool? Decision { get; set; } // true = approved, false = rejected, null = pending

        [StringLength(500)]
        public string? Comments { get; set; }

        public DateTime? ApprovalDate { get; set; }

        // Navigation properties
        [ForeignKey("ClaimId")]
        public virtual MonthlyClaim MonthlyClaim { get; set; } = null!;
    }
}