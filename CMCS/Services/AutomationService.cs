using CMCS.Data;
using CMCS.Models;
using Microsoft.EntityFrameworkCore;

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

        // Simple thresholds - change as required
        private const decimal ManagerAutoApproveAmountThreshold = 20000m;
        private const decimal VerifierAutoVerifyHoursThreshold = 50m;
        private const decimal VerifierAutoVerifyAmountThreshold = 5000m;

        public AutomationService(ApplicationDbContext db, ILogger<AutomationService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Run a single pass of automation rules. Lightweight and synchronous.
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
                    // 1) Auto-verify rule (only for Submitted)
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

                    // Reload approvals if we changed status in this pass
                    // 2) Auto-approve rule (only for Verify)
                    if (claim.Status == ClaimStatus.Verify)
                    {
                        // Example: if total amount is small enough, auto-approve by manager
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

                    // if some rule rejected (none by default) - example placeholder:
                    // if (someCondition) { claim.Status = ClaimStatus.Rejected; ... }
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

            var sb = new System.Text.StringBuilder();
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
    }
}
