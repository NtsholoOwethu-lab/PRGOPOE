using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMCS.Models
{
    public enum ClaimStatus
    {
        Draft,
        Submitted,
        UnderReview,
        Approved,
        Rejected,
        Paid
    }

    public class MonthlyClaim
    {
        [Key]
        public int ClaimId { get; set; }

        [Required]
        public int LecturerId { get; set; }

        [Required]
        [Range(1, 12)]
        public int Month { get; set; }

        [Required]
        [Range(2020, 2030)]
        public int Year { get; set; }

        [Required]
        [Range(0, 200)]
        public decimal TotalHours { get; set; }

        [Required]
        [Range(0, 100000)]
        public decimal TotalAmount { get; set; }

        [Required]
        public ClaimStatus Status { get; set; } = ClaimStatus.Draft;

        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("LecturerId")]
        public virtual Lecturer Lecturer { get; set; } = null!;
        public virtual ICollection<SupportingDocument> SupportingDocuments { get; set; } = new List<SupportingDocument>();
        public virtual ICollection<ClaimApproval> ClaimApprovals { get; set; } = new List<ClaimApproval>();

        // Helper property for file upload
        [NotMapped]
        public IFormFileCollection? Files { get; set; }
    }
}