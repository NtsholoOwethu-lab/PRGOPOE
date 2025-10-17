using Microsoft.EntityFrameworkCore;
using CMCS.Data;
using CMCS.Models;

namespace CMCS.Repositories
{
    public class ClaimRepository : IClaimRepository
    {
        private readonly ApplicationDbContext _context;

        public ClaimRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MonthlyClaim>> GetClaimsByLecturerAsync(int lecturerId)
        {
            return await _context.MonthlyClaims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .Include(c => c.ClaimApprovals)
                .Where(c => c.LecturerId == lecturerId)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        public async Task<MonthlyClaim?> GetClaimByIdAsync(int claimId)
        {
            return await _context.MonthlyClaims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .Include(c => c.ClaimApprovals)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);
        }

        public async Task<MonthlyClaim> CreateClaimAsync(MonthlyClaim claim)
        {
            claim.SubmissionDate = DateTime.Now;
            claim.Status = ClaimStatus.Submitted;

            _context.MonthlyClaims.Add(claim);
            await _context.SaveChangesAsync();
            return claim;
        }

        public async Task UpdateClaimAsync(MonthlyClaim claim)
        {
            _context.MonthlyClaims.Update(claim);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteClaimAsync(int claimId)
        {
            var claim = await _context.MonthlyClaims.FindAsync(claimId);
            if (claim == null) return false;

            _context.MonthlyClaims.Remove(claim);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<MonthlyClaim>> GetPendingClaimsAsync()
        {
            return await _context.MonthlyClaims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .Where(c => c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderReview)
                .OrderBy(c => c.SubmissionDate)
                .ToListAsync();
        }

        public async Task<bool> ApproveClaimAsync(int claimId, int approverId, ApproverType approverType, string? comments = null)
        {
            var claim = await _context.MonthlyClaims.FindAsync(claimId);
            if (claim == null) return false;

            var approval = new ClaimApproval
            {
                ClaimId = claimId,
                ApproverType = approverType,
                ApproverId = approverId,
                Decision = true,
                Comments = comments,
                ApprovalDate = DateTime.Now
            };

            _context.ClaimApprovals.Add(approval);

            // Update claim status based on approval level
            if (approverType == ApproverType.ProgrammeCoordinator)
            {
                claim.Status = ClaimStatus.UnderReview;
            }
            else if (approverType == ApproverType.AcademicManager)
            {
                claim.Status = ClaimStatus.Approved;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectClaimAsync(int claimId, int approverId, ApproverType approverType, string? comments = null)
        {
            var claim = await _context.MonthlyClaims.FindAsync(claimId);
            if (claim == null) return false;

            var approval = new ClaimApproval
            {
                ClaimId = claimId,
                ApproverType = approverType,
                ApproverId = approverId,
                Decision = false,
                Comments = comments,
                ApprovalDate = DateTime.Now
            };

            _context.ClaimApprovals.Add(approval);
            claim.Status = ClaimStatus.Rejected;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AddSupportingDocumentsAsync(int claimId, List<SupportingDocument> documents)
        {
            foreach (var document in documents)
            {
                document.ClaimId = claimId;
                _context.SupportingDocuments.Add(document);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<SupportingDocument?> GetDocumentByIdAsync(int documentId)
        {
            return await _context.SupportingDocuments
                .Include(d => d.MonthlyClaim)
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);
        }
    }
}