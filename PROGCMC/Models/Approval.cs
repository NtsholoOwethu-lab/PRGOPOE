using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PROGCMC.Models
{
    public class Approval
    {

        public int ApprovalID { get; set; }
        public int ClaimID { get; set; }
        public int UserID { get; set; } // Approver (Programme Coordinator / Academic Manager)
        public DateTime ApprovalDate { get; set; } = DateTime.Now;
        public string Decision { get; set; } // Approved / Rejected / Pending
        public string Comments { get; set; }

    }
}
