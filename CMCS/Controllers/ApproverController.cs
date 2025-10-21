using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMCS.Models;
using CMCS.Data;

namespace CMCS.Controllers
{
    public class ApproverController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ApproverController(ApplicationDbContext context)
        {
            _context = context;
        }

        // === DASHBOARD ===
        public async Task<IActionResult> Dashboard()
        {
            var pendingClaims = await _context.MonthlyClaims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .Where(c => c.Status == ClaimStatus.Verify)
                .OrderBy(c => c.SubmissionDate)
                .ToListAsync();

            return View(pendingClaims);
        }

        // === REVIEW CLAIM ===
        public async Task<IActionResult> ReviewClaim(int id)
        {
            var claim = await _context.MonthlyClaims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .Include(c => c.ClaimApprovals)
                .FirstOrDefaultAsync(c => c.ClaimId == id);

            if (claim == null)
                return NotFound();

            return View(claim);
        }

        // === APPROVE CLAIM ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveClaim(int claimId, string? comments, ApproverType approverType)
        {
            try
            {
                var claim = await _context.MonthlyClaims.FindAsync(claimId);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Dashboard));
                }

                // Record approval
                var approval = new ClaimApproval
                {
                    ClaimId = claimId,
                    ApproverType = approverType,
                    ApproverId = 1, // demo user ID
                    Decision = true,
                    Comments = comments,
                    ApprovalDate = DateTime.Now
                };

                _context.ClaimApprovals.Add(approval);

                // Update claim status
                if (approverType == ApproverType.ProgrammeCoordinator)
                    claim.Status = ClaimStatus.Approved;
                else if (approverType == ApproverType.AcademicManager)
                    claim.Status = ClaimStatus.Approved;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Claim approved successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Dashboard));
        }

        // === REJECT CLAIM ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectClaim(int claimId, string? comments, ApproverType approverType)
        {
            try
            {
                var claim = await _context.MonthlyClaims.FindAsync(claimId);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Dashboard));
                }

                // Record rejection
                var approval = new ClaimApproval
                {
                    ClaimId = claimId,
                    ApproverType = approverType,
                    ApproverId = 1,
                    Decision = false,
                    Comments = comments,
                    ApprovalDate = DateTime.Now
                };

                _context.ClaimApprovals.Add(approval);
                claim.Status = ClaimStatus.Rejected;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Claim rejected successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Dashboard));
        }

        // === CLAIM HISTORY ===
        public async Task<IActionResult> ClaimHistory()
        {
            var claims = await _context.MonthlyClaims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .Include(c => c.ClaimApprovals)
                .Where(c => c.LecturerId == 1)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            return View(claims);
        }
    }
}
