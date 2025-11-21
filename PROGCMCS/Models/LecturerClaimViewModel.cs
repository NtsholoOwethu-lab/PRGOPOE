using System.ComponentModel.DataAnnotations;

namespace PROGCMCS.Models
{
    public class LecturerClaimViewModel
    {
        public int ClaimId { get; set; }

        [Required]
        [Range(1, 12, ErrorMessage = "Month must be between 1 and 12.")]
        public int Month { get; set; } = DateTime.Now.Month;

        [Required]
        [Range(2020, 2030, ErrorMessage = "Year must be between 2020 and 2030.")]
        public int Year { get; set; } = DateTime.Now.Year;

        [Required]
        [Display(Name = "Total Hours Worked")]
        [Range(0.5, 180, ErrorMessage = "Hours must be between 0.5 and 180.")]
        public decimal TotalHours { get; set; }

        [Display(Name = "Hourly Rate")]
        public decimal HourlyRate { get; set; }

        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Maximum Monthly Hours")]
        public decimal MaxMonthlyHours { get; set; } = 180;

        [Display(Name = "Remaining Hours")]
        public decimal RemainingHours { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Auto-calculated display fields
        public string DisplayTotalAmount => TotalAmount.ToString("C");
        public bool ExceedsMonthlyLimit => TotalHours > MaxMonthlyHours;
    }

    public class LecturerClaimsListViewModel
    {
        public int ClaimId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public ClaimStatus Status { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string? Notes { get; set; }

        // Formatted properties for display
        public string Period => $"{Month}/{Year}";
        public string DisplayAmount => TotalAmount.ToString("C");
        public string DisplaySubmissionDate => SubmissionDate.ToString("yyyy-MM-dd");
        public string StatusBadgeClass => Status switch
        {
            ClaimStatus.Draft => "bg-secondary",
            ClaimStatus.Submitted => "bg-primary",
            ClaimStatus.Verify => "bg-warning",
            ClaimStatus.Approved => "bg-success",
            ClaimStatus.Rejected => "bg-danger",
            ClaimStatus.Paid => "bg-info",
            _ => "bg-secondary"
        };
    }
}