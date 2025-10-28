
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
            // Verification/approval thresholds
        private const decimal HourlyRateMin = 50m;
        private const decimal HourlyRateMax = 500m;
        private const decimal TotalHoursMin = 2m;
        private const decimal TotalHoursMax = 18m;
        private const int MaxMonthsBack = 12; // optional: claims older than this are invalid

       
            // Verification/approval thresholds
        
        

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
                var claim = await _context.MonthlyClaims
                    .Include(c => c.Lecturer)
                    .Include(c => c.ClaimApprovals)
                    .FirstOrDefaultAsync(c => c.ClaimId == claimId);

                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Dashboard));
                }

                // Run validation rules before approving
                var violations = GetParameterViolations(claim);
                if (violations.Any())
                {
                    // record auto-decline
                    claim.Status = ClaimStatus.Rejected;

                    var declineApproval = new ClaimApproval
                    {
                        ClaimId = claimId,
                        ApproverType = approverType,
                        ApproverId = GetCurrentApproverId(), // helper below
                        Decision = false,
                        Comments = "Auto-declined: " + string.Join("; ", violations),
                        ApprovalDate = DateTime.Now
                    };

                    _context.ClaimApprovals.Add(declineApproval);
                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] = "Claim declined due to: " + string.Join("; ", violations);
                    return RedirectToAction(nameof(Dashboard));
                }

                // No violations — proceed to approve
                var approval = new ClaimApproval
                {
                    ClaimId = claimId,
                    ApproverType = approverType,
                    ApproverId = GetCurrentApproverId(),
                    Decision = true,
                    Comments = comments,
                    ApprovalDate = DateTime.Now
                };

                _context.ClaimApprovals.Add(approval);

                // Update claim status (keeps previous behaviour)
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
        private List<string> GetParameterViolations(MonthlyClaim claim)
        {
            var violations = new List<string>();

            // hourly rate
            if (claim.HourlyRate < HourlyRateMin || claim.HourlyRate > HourlyRateMax)
                violations.Add($"Hourly rate ({claim.HourlyRate}) must be between {HourlyRateMin} and {HourlyRateMax}.");

            // total hours
            if (claim.TotalHours < TotalHoursMin || claim.TotalHours > TotalHoursMax)
                violations.Add($"Total hours ({claim.TotalHours}) must be between {TotalHoursMin} and {TotalHoursMax}.");

            // month/year: not in future and not older than MaxMonthsBack months
            try
            {
                var claimPeriod = new DateTime(claim.Year, claim.Month, 1);
                var now = DateTime.Now;
                if (claimPeriod > new DateTime(now.Year, now.Month, 1))
                    violations.Add("Claim period cannot be in the future.");

                var monthsDiff = ((now.Year - claimPeriod.Year) * 12) + (now.Month - claimPeriod.Month);
                if (monthsDiff > MaxMonthsBack)
                    violations.Add($"Claim period is older than {MaxMonthsBack} months.");
            }
            catch
            {
                violations.Add("Claim period invalid.");
            }

            return violations;
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
        // requirement for approval
        private int GetCurrentApproverId()
        {
            var idClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var id))
                return id;
            return 1; // demo fallback
        }

    }
}
