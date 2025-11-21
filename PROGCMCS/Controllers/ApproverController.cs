using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PROGCMCS.Data;
using PROGCMCS.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace PROGCMCS.Controllers
{
    [Authorize(Roles = "Coordinator,Manager")]
    public class ApproverController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ApproverController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);

            var pendingClaims = await _context.MonthlyClaims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.Verify)
                .OrderBy(c => c.SubmissionDate)
                .ToListAsync();

            ViewBag.UserRole = userRoles.FirstOrDefault();
            return View(pendingClaims);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveClaim(int claimId, string notes)
        {
            return await ProcessClaim(claimId, true, notes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectClaim(int claimId, string notes)
        {
            return await ProcessClaim(claimId, false, notes);
        }

        private async Task<IActionResult> ProcessClaim(int claimId, bool isApproved, string notes)
        {
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = userRoles.FirstOrDefault();

            var claim = await _context.MonthlyClaims
                .Include(c => c.Lecturer)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Create approval record
            var approval = new ClaimApproval
            {
                ClaimId = claimId,
                ApproverId = user.Id, // Use the actual user ID
                ApproverRole = userRole ?? "Approver",
                IsApproved = isApproved,
                Notes = notes,
                ApprovalDate = DateTime.Now
            };

            _context.ClaimApprovals.Add(approval);

            // Update claim status based on role and approval
            if (userRole == "Coordinator")
            {
                claim.Status = isApproved ? ClaimStatus.Verify : ClaimStatus.Rejected;
            }
            else if (userRole == "Manager")
            {
                claim.Status = isApproved ? ClaimStatus.Approved : ClaimStatus.Rejected;
            }

            await _context.SaveChangesAsync();

            var action = isApproved ? "approved" : "rejected";
            TempData["SuccessMessage"] = $"Claim {action} successfully.";
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> ClaimDetails(int id)
        {
            var claim = await _context.MonthlyClaims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .Include(c => c.ClaimApprovals)
                .FirstOrDefaultAsync(c => c.ClaimId == id);

            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction(nameof(Dashboard));
            }

            return View(claim);
        }
    }
}