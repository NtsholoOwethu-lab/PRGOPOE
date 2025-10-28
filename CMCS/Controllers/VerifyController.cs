using CMCS.Data;
using CMCS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Controllers
{
    public class VerifyController : Controller
    {
        private readonly ApplicationDbContext _context;


        // Verification policy constants (tweak as needed)
        private const decimal HourlyRateMin = 50m;
        private const decimal HourlyRateMax = 300m;
        private const decimal TotalHoursMin = 2m;
        private const decimal TotalHoursMax = 10m;
        private const int MaxMonthsBack = 12; // claims older than this (in months) will be declined


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
                var claim = await _context.MonthlyClaims
                    .Include(c => c.Lecturer)
                    .Include(c => c.ClaimApprovals)
                    .FirstOrDefaultAsync(c => c.ClaimId == claimId);

                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Dashboard));
                }

                // Run parameter validation rules
                var violations = GetParameterViolations(claim);

                if (violations.Any())
                {
                    // Auto-decline the claim with detailed comments
                    claim.Status = ClaimStatus.Rejected;

                    var approval = new ClaimApproval
                    {
                        ClaimId = claimId,
                        ApproverType = ApproverType.ProgrammeCoordinator,
                        ApproverId = 1, // demo verifier id or replace with actual user id
                        Decision = false,
                        Comments = "Auto-declined by verification rules: " + string.Join("; ", violations),
                        ApprovalDate = DateTime.Now
                    };

                    _context.ClaimApprovals.Add(approval);
                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] = "Claim declined due to verification rules: " + string.Join("; ", violations);
                    return RedirectToAction(nameof(Dashboard));
                }

                // No violations → mark claim as verified and ready for approval
                claim.Status = ClaimStatus.Verify;

                var verifyApproval = new ClaimApproval
                {
                    ClaimId = claimId,
                    ApproverType = ApproverType.ProgrammeCoordinator,
                    ApproverId = 1, // demo verifier user id
                    Decision = true,
                    Comments = comments,
                    ApprovalDate = DateTime.Now
                };

                _context.ClaimApprovals.Add(verifyApproval);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Claim verified successfully and sent for approval!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error verifying claim: {ex.Message}";
            }

            return RedirectToAction(nameof(Dashboard));
        }

        // Helper: check claim parameters and return list of violation messages
        private List<string> GetParameterViolations(MonthlyClaim claim)
        {
            var violations = new List<string>();

            // hourly rate
            if (claim.HourlyRate < HourlyRateMin || claim.HourlyRate > HourlyRateMax)
            {
                violations.Add($"Hourly rate ({claim.HourlyRate}) must be between {HourlyRateMin} and {HourlyRateMax}.");
            }

            // total hours
            if (claim.TotalHours < TotalHoursMin || claim.TotalHours > TotalHoursMax)
            {
                violations.Add($"Total hours ({claim.TotalHours}) must be between {TotalHoursMin} and {TotalHoursMax}.");
            }

            // claim period (month/year) validation
            try
            {
                // Take the first day of the claimed month for date comparison
                var claimPeriod = new DateTime(claim.Year, claim.Month, 1);
                var now = DateTime.Now;
                if (claimPeriod > new DateTime(now.Year, now.Month, 1))
                {
                    violations.Add("Claim period cannot be in the future.");
                }

                // If older than MaxMonthsBack months (approx)
                var monthsDiff = ((now.Year - claimPeriod.Year) * 12) + (now.Month - claimPeriod.Month);
                if (monthsDiff > MaxMonthsBack)
                {
                    violations.Add($"Claim period is older than {MaxMonthsBack} months.");
                }
            }
            catch
            {
                // If month/year combination is invalid (shouldn't happen if model validation works)
                violations.Add("Claim period is invalid.");
            }

            return violations;
        }

        // === DECLINE CLAIM === (unchanged or keep as-is)
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
    } //approverid = 1 is a demo
}
