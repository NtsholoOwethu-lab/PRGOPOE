using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PROGCMC.Models
{
    public class Claim
    {
        public int ClaimID { get; set; }
        public int LecturerID { get; set; }
        public DateTime ClaimDate { get; set; } = DateTime.Today;
        public decimal PayPerHour { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal TotalAmount => PayPerHour * HoursWorked;
        public string Status { get; set; } = "Submitted"; // Submitted, PendingApproval, Approved, Rejected
        public List<SupportingDocument> Documents { get; set; } = new();
        public List<Approval> Approvals { get; set; } = new();
    }
}
