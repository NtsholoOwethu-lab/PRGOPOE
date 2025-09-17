using Microsoft.AspNetCore.Mvc;
using PROGCMC.Models;

namespace PROGCMC.Controllers
{
    public class ClaimsController : Controller
    {
        // NOTE: In-memory demo store. Replace with DB/repository later.
        private static List<Lecturer> _lecturers = new() {
            new Lecturer { LecturerID = 1, Name = "Thabo", Surname = "M", EmailAddress = "thabo@example.com" }
        };

        private static List<User> _users = new() {
            new User { UserID = 1, Name = "Sally", Surname = "P", Position = "Programme Coordinator" },
            new User { UserID = 2, Name = "John", Surname = "D", Position = "Academic Manager" }
        };

        private static List<Claim> _claims = new() {
            new Claim {
                ClaimID = 1,
                LecturerID = 1,
                ClaimDate = DateTime.Today.AddDays(-10),
                PayPerHour = 250,
                HoursWorked = 10,
                Status = "PendingApproval",
                Documents = new List<SupportingDocument> {
                    new SupportingDocument { DocumentID = 1, ClaimID = 1, FileName = "timesheet.pdf" }
                },
                Approvals = new List<Approval> {
                    new Approval { ApprovalID = 1, ClaimID = 1, UserID = 1, Decision = "Pending", Comments = "Awaiting review" }
                }
            }
        };

        // GET: /Claims/Create  -> Claim Submission form
        public IActionResult Create()
        {
            ViewBag.Lecturers = _lecturers;
            return View(new Claim { ClaimDate = DateTime.Today });
        }

        // POST: /Claims/Create (non-functional - just adds to in-memory)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Claim model)
        {
            model.ClaimID = _claims.Any() ? _claims.Max(c => c.ClaimID) + 1 : 1;
            model.Status = "Submitted";
            model.Documents = new List<SupportingDocument>(); // Document upload handled client-side in this prototype
            _claims.Add(model);
            TempData["Message"] = "Claim submitted (prototype).";
            return RedirectToAction(nameof(MyClaims));
        }

        // GET: /Claims/MyClaims
        public IActionResult MyClaims(int lecturerId = 1)
        {
            var claims = _claims.Where(c => c.LecturerID == lecturerId).OrderByDescending(c => c.ClaimDate).ToList();
            return View(claims);
        }

        // GET: /Claims/ApprovalDashboard
        public IActionResult ApprovalDashboard(string filter = "All")
        {
            var list = _claims.AsEnumerable();
            if (filter == "Pending") list = list.Where(c => c.Status.Contains("Pending") || c.Status == "Submitted");
            ViewBag.Filter = filter;
            ViewBag.Users = _users;
            return View(list.ToList());
        }

        // POST: /Claims/Approve (simple in-memory approval)
        [HttpPost]
        public IActionResult Approve(int claimId, int userId, string decision, string comments)
        {
            var claim = _claims.FirstOrDefault(c => c.ClaimID == claimId);
            if (claim != null)
            {
                var approval = new Approval
                {
                    ApprovalID = claim.Approvals.Any() ? claim.Approvals.Max(a => a.ApprovalID) + 1 : 1,
                    ClaimID = claimId,
                    UserID = userId,
                    Decision = decision,
                    Comments = comments,
                    ApprovalDate = DateTime.Now
                };
                claim.Approvals.Add(approval);
                claim.Status = decision == "Approved" ? "Approved" : "Rejected";
            }
            return RedirectToAction(nameof(ApprovalDashboard));
        }

        // GET Document partial
        public IActionResult DocumentUploadPartial(int claimId = 0)
        {
            ViewBag.ClaimID = claimId;
            return PartialView("_DocumentUploadPartial");
        }

        public IActionResult Submit()
        {
            return View();
        }
    }
}
