using CMCS.Data;
using CMCS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace CMCS.Services
{
    public class AutomationResult
    {
        public int VerifiedCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public decimal TotalApprovedAmount { get; set; }
        public List<string> Messages { get; set; } = new();
    }

    public class AutomationService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AutomationService> _logger;

        // Auto rules thresholds (tweak as required)
        private const decimal ManagerAutoApproveAmountThreshold = 20000m;
        private const decimal VerifierAutoVerifyHoursThreshold = 50m;
        private const decimal VerifierAutoVerifyAmountThreshold = 5000m;

        // Validation / approval policy (single source of truth)
        private const decimal HourlyRateMin = 50m;
        private const decimal HourlyRateMax = 500m;
        private const decimal TotalHoursMin = 2m;
        private const decimal TotalHoursMax = 18m;
        private const int MaxMonthsBack = 12; // claims older than this (in months) are invalid

        public AutomationService(ApplicationDbContext db, ILogger<AutomationService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Run a single pass of automation rules. Lightweight and transactional.
        /// - First applies validation (auto-decline) for Submitted/Verify claims
        /// - Then runs auto-verify and auto-approve rules for eligible claims
        /// </summary>
        public async Task<AutomationResult> RunAutomationAsync(CancellationToken cancellation = default)
        {
            var result = new AutomationResult();

            // We'll process claims in Submitted or Verify status
            var claims = await _db.MonthlyClaims
                .Include(c => c.Lecturer)
                .Include(c => c.ClaimApprovals)
                .Where(c => c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.Verify)
                .ToListAsync(cancellation);

            using var tx = await _db.Database.BeginTransactionAsync(cancellation);
            try
            {
                foreach (var claim in claims)
                {
                    // --- 0) First, run validation rules (same as verifier/approver)
                    var violations = GetParameterViolations(claim);
                    if (violations.Any())
                    {
                        // Auto-decline and record reason
                        claim.Status = ClaimStatus.Rejected;

                        var declineApproval = new ClaimApproval
                        {
                            ClaimId = claim.ClaimId,
                            ApproverType = ApproverType.ProgrammeCoordinator,
                            ApproverId = 0, // system
                            Decision = false,
                            Comments = "Auto-declined by automation rules: " + string.Join("; ", violations),
                            ApprovalDate = DateTime.UtcNow
                        };
                        _db.ClaimApprovals.Add(declineApproval);
                        result.RejectedCount++;
                        result.Messages.Add($"Auto-declined claim {claim.ClaimId}: {string.Join("; ", violations)}");

                        // Skip any further processing for this claim
                        continue;
                    }

                    // --- 1) Auto-verify rule (only for Submitted)
                    if (claim.Status == ClaimStatus.Submitted)
                    {
                        if (claim.TotalHours <= VerifierAutoVerifyHoursThreshold
                            && claim.TotalAmount <= VerifierAutoVerifyAmountThreshold)
                        {
                            claim.Status = ClaimStatus.Verify;
                            var approval = new ClaimApproval
                            {
                                ClaimId = claim.ClaimId,
                                ApproverType = ApproverType.ProgrammeCoordinator,
                                ApproverId = 0, // system
                                Decision = true,
                                Comments = "Auto-verified by automation rules.",
                                ApprovalDate = DateTime.UtcNow
                            };
                            _db.ClaimApprovals.Add(approval);
                            result.VerifiedCount++;
                            result.Messages.Add($"Auto-verified claim {claim.ClaimId}.");
                        }
                    }

                    // --- 2) Auto-approve rule (only for Verify)
                    if (claim.Status == ClaimStatus.Verify)
                    {
                        if (claim.TotalAmount <= ManagerAutoApproveAmountThreshold)
                        {
                            claim.Status = ClaimStatus.Approved;
                            var approval = new ClaimApproval
                            {
                                ClaimId = claim.ClaimId,
                                ApproverType = ApproverType.AcademicManager,
                                ApproverId = 0,
                                Decision = true,
                                Comments = "Auto-approved by automation rules.",
                                ApprovalDate = DateTime.UtcNow
                            };
                            _db.ClaimApprovals.Add(approval);
                            result.ApprovedCount++;
                            result.TotalApprovedAmount += claim.TotalAmount;
                            result.Messages.Add($"Auto-approved claim {claim.ClaimId}.");
                        }
                    }
                }

                await _db.SaveChangesAsync(cancellation);
                await tx.CommitAsync(cancellation);

                result.Messages.Add($"Processed {claims.Count} claims.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running automation");
                await tx.RollbackAsync(cancellation);
                result.Messages.Add("Automation failed: " + ex.Message);
                throw;
            }

            return result;
        }

        /// <summary>
        /// Build CSV report for approved claims in a date range (nullable range => all).
        /// Returns CSV text (caller streams it).
        /// </summary>
        public async Task<string> BuildApprovedClaimsCsvAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellation = default)
        {
            var query = _db.MonthlyClaims
                .Include(c => c.Lecturer)
                .Include(c => c.ClaimApprovals)
                .Where(c => c.Status == ClaimStatus.Approved)
                .AsQueryable();

            if (from.HasValue) query = query.Where(c => c.SubmissionDate >= from.Value);
            if (to.HasValue) query = query.Where(c => c.SubmissionDate <= to.Value);

            var claims = await query.OrderBy(c => c.SubmissionDate).ToListAsync(cancellation);

            var sb = new StringBuilder();
            sb.AppendLine("ClaimId,Lecturer,Email,Month,Year,TotalHours,HourlyRate,TotalAmount,ApprovalDate,Status");

            foreach (var c in claims)
            {
                var approvDate = c.ClaimApprovals.OrderByDescending(a => a.ApprovalDate).FirstOrDefault()?.ApprovalDate;
                sb.AppendLine(string.Join(",",
                    c.ClaimId,
                    QuoteCsv($"{c.Lecturer?.FirstName} {c.Lecturer?.LastName}"),
                    QuoteCsv(c.Lecturer?.Email ?? ""),
                    c.Month,
                    c.Year,
                    c.TotalHours,
                    c.HourlyRate,
                    c.TotalAmount,
                    approvDate?.ToString("yyyy-MM-dd") ?? "",
                    c.Status.ToString()
                ));
            }

            return sb.ToString();

            static string QuoteCsv(string s)
            {
                if (s == null) s = "";
                return $"\"{s.Replace("\"", "\"\"")}\"";
            }
        }

        /// <summary>
        /// Return parameter violations for a claim (empty list => no violations).
        /// This mirrors the verifier/approver rules (single source of truth).
        /// </summary>
        private List<string> GetParameterViolations(MonthlyClaim claim)
        {
            var violations = new List<string>();

            // hourly rate
            if (claim.HourlyRate < HourlyRateMin || claim.HourlyRate > HourlyRateMax)
                violations.Add($"Hourly rate ({claim.HourlyRate}) must be between {HourlyRateMin} and {HourlyRateMax}.");

            // total hours
            if (claim.TotalHours < TotalHoursMin || claim.TotalHours > TotalHoursMax)
                violations.Add($"Total hours ({claim.TotalHours}) must be between {TotalHoursMin} and {TotalHoursMax}.");

            // claim period (month/year)
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
    }
}
