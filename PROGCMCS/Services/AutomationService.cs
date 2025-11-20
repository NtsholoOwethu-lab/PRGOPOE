using System.Reflection.Metadata;
using System.Text;
using PROGCMCS.Data;
using PROGCMCS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PROGCMCS.Data;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Document = QuestPDF.Fluent.Document;

namespace PROGCMCS.Services
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

        // Automation thresholds
        private const decimal ManagerAutoApproveAmountThreshold = 20000m;
        private const decimal VerifierAutoVerifyHoursThreshold = 50m;
        private const decimal VerifierAutoVerifyAmountThreshold = 5000m;

        public AutomationService(ApplicationDbContext db, ILogger<AutomationService> logger)
        {
            _db = db;
            _logger = logger;
        }


        // 1️⃣ AUTOMATION PROCESS

        public async Task<AutomationResult> RunAutomationAsync(CancellationToken cancellation = default)
        {
            var result = new AutomationResult();

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
                    // Validate claim before processing
                    var validationMessage = ValidateClaim(claim);
                    if (validationMessage != null)
                    {
                        claim.Status = ClaimStatus.Rejected;
                        result.RejectedCount++;
                        result.Messages.Add($"Claim {claim.ClaimId} rejected: {validationMessage}");
                        continue;
                    }

                    // Auto-verify rule
                    if (claim.Status == ClaimStatus.Submitted)
                    {
                        if (claim.TotalHours <= VerifierAutoVerifyHoursThreshold &&
                            claim.TotalAmount <= VerifierAutoVerifyAmountThreshold)
                        {
                            claim.Status = ClaimStatus.Verify;
                            result.VerifiedCount++;
                            result.Messages.Add($"Auto-verified claim {claim.ClaimId}.");
                        }
                    }

                    // Auto-approve rule
                    if (claim.Status == ClaimStatus.Verify)
                    {
                        if (claim.TotalAmount <= ManagerAutoApproveAmountThreshold)
                        {
                            claim.Status = ClaimStatus.Approved;
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


        // 2️⃣ CLAIM VALIDATION

        private string ValidateClaim(MonthlyClaim claim)
        {
            if (claim.HourlyRate < 50 || claim.HourlyRate > 500)
                return "Hourly rate must be between 50 and 500.";

            if (claim.TotalHours < 2 || claim.TotalHours > 18)
                return "Total claimed hours must be between 2 and 18.";

            if (claim.Month > DateTime.Now.Month && claim.Year == DateTime.Now.Year)
                return "Claim month cannot be in the future.";

            return null; // Valid claim
        }


        // 3️⃣ CSV REPORT GENERATION

        public async Task<string> BuildApprovedClaimsCsvAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellation = default)
        {
            var query = _db.MonthlyClaims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Approved);

            if (from.HasValue) query = query.Where(c => c.SubmissionDate >= from.Value);
            if (to.HasValue) query = query.Where(c => c.SubmissionDate <= to.Value);

            var claims = await query.OrderBy(c => c.SubmissionDate).ToListAsync(cancellation);

            var sb = new StringBuilder();
            sb.AppendLine("ClaimId,Lecturer,Email,Month,Year,TotalHours,HourlyRate,TotalAmount,Status");

            foreach (var c in claims)
            {
                sb.AppendLine($"{c.ClaimId},{c.Lecturer?.FirstName} {c.Lecturer?.LastName},{c.Lecturer?.Email},{c.Month},{c.Year},{c.TotalHours},{c.HourlyRate},{c.TotalAmount},{c.Status}");
            }

            return sb.ToString();
        }


        // 4️⃣ INVOICE PDF GENERATION

        public async Task<byte[]> BuildInvoicePdfAsync(int claimId)
        {
            var claim = await _db.MonthlyClaims
                .Include(c => c.Lecturer)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null)
                throw new ArgumentException("Claim not found.");

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Invoice for Claim #{claim.ClaimId}").Bold().FontSize(20);
                        col.Item().Text($"Lecturer: {claim.Lecturer.FirstName} {claim.Lecturer.LastName}");
                        col.Item().Text($"Email: {claim.Lecturer.Email}");
                        col.Item().Text($"Period: {claim.Month}/{claim.Year}");
                        col.Item().Text($"Hours: {claim.TotalHours} × R{claim.HourlyRate:N2}");
                        col.Item().Text($"Total: R{claim.TotalAmount:N2}").Bold();
                        col.Item().PaddingTop(20).LineHorizontal(1);
                        col.Item().PaddingTop(10).Text("Thank you for your contribution!");
                    });
                });
            });

            using var ms = new MemoryStream();
            doc.GeneratePdf(ms);
            return ms.ToArray();
        }
    }
}
