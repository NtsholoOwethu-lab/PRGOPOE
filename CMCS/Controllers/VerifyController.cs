using CMCS.Data;
using CMCS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Controllers
{
    public class VerifyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VerifyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // === DASHBOARD ===
        public async Task<IActionResult> Dashboard()
        {
            // Claims that were submitted and need verification
            var claimsToVerify = await _context.MonthlyClaims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .Where(c => c.Status == ClaimStatus.Submitted)
                .OrderBy(c => c.SubmissionDate)
                .ToListAsync();

            return View(claimsToVerify);
        }

        // === REVIEW CLAIM ===
        public async Task<IActionResult> ReviewClaim(int id)
        {
            var claim = await _context.MonthlyClaims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .FirstOrDefaultAsync(c => c.ClaimId == id);

            if (claim == null)
                return NotFound();

            return View(claim);
        }

        // === VERIFY CLAIM ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyClaim(int claimId, string? comments)
        {
            try
            {
                var claim = await _context.MonthlyClaims.FindAsync(claimId);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Dashboard));
                }

                // Mark claim as verified and ready for approval
                claim.Status = ClaimStatus.Verify;

                var approval = new ClaimApproval
                {
                    ClaimId = claimId,
                    ApproverType = ApproverType.ProgrammeCoordinator,
                    ApproverId = 1, // demo verifier user
                    Decision = true,
                    Comments = comments,
                    ApprovalDate = DateTime.Now
                };

                _context.ClaimApprovals.Add(approval);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Claim verified successfully and sent for approval!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error verifying claim: {ex.Message}";
            }

            return RedirectToAction(nameof(Dashboard));
        }

        // === DECLINE CLAIM ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineClaim(int claimId, string? comments)
        {
            try
            {
                var claim = await _context.MonthlyClaims.FindAsync(claimId);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Dashboard));
                }

                claim.Status = ClaimStatus.Rejected;

                var approval = new ClaimApproval
                {
                    ClaimId = claimId,
                    ApproverType = ApproverType.ProgrammeCoordinator,
                    ApproverId = 1,
                    Decision = false,
                    Comments = comments,
                    ApprovalDate = DateTime.Now
                };

                _context.ClaimApprovals.Add(approval);
                await _context.SaveChangesAsync();

                TempData["ErrorMessage"] = "Claim declined and not sent for approval.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error declining claim: {ex.Message}";
            }

            return RedirectToAction(nameof(Dashboard));
        }
    }
}
