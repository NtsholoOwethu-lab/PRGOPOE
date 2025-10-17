using CMCS.Models;

namespace CMCS.Repositories
{
    public interface IClaimRepository
    {
        Task<IEnumerable<MonthlyClaim>> GetClaimsByLecturerAsync(int lecturerId);
        Task<MonthlyClaim?> GetClaimByIdAsync(int claimId);
        Task<MonthlyClaim> CreateClaimAsync(MonthlyClaim claim);
        Task UpdateClaimAsync(MonthlyClaim claim);
        Task<bool> DeleteClaimAsync(int claimId);
        Task<IEnumerable<MonthlyClaim>> GetPendingClaimsAsync();
        Task<bool> ApproveClaimAsync(int claimId, int approverId, ApproverType approverType, string? comments = null);
        Task<bool> RejectClaimAsync(int claimId, int approverId, ApproverType approverType, string? comments = null);
        Task AddSupportingDocumentsAsync(int claimId, List<SupportingDocument> documents);
        Task<SupportingDocument?> GetDocumentByIdAsync(int documentId);
    }
}